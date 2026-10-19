using System.Collections.Immutable;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>
/// The engine: a world, where it is in the round, and what it will ask next.
/// </summary>
/// <remarks>
/// The game owns phase progression and the pending decision. Rules procedures
/// and authored abilities share its agenda so a decision can suspend either
/// operation without losing the surrounding timing window.
/// </remarks>
public sealed class Game
{
    /// <summary>The affordance verb for keeping or replacing an opening hand.</summary>
    public const string ResolveMulligans = "Resolve Mulligans";

    /// <summary>The affordance verb for flipping between hero and alter-ego.</summary>
    public const string ChangeForm = "Change_Form";

    /// <summary>The verb a triggered "Action" carries — <c>rr:player-turn.5</c>.</summary>
    public const string ActionVerb = "Action";

    /// <summary>The affordance verb for resolving a turn's end phase.</summary>
    public const string EndPhaseVerb = "End Phase";

    // The engine's `message_name` at each of the three prompts. `End Turn` is
    // not spelled like the others and that is not a transcription slip -- the
    // other two are timing points and it is a message name. Recorded as
    // measured rather than regularised.
    internal const string MulliganTrigger = "WhenPlayerChooseAbility";
    internal const string TurnTrigger = "WhenPlayerInTurn";
    internal const string EndPhaseTrigger = "End Turn";

    private static readonly ImmutableArray<string> Derived =
        [ResolveMulligans, ChangeForm, EndPhaseVerb];

    // An affordance id is a handle, and the property that matters is that the
    // same option re-offered keeps it: a client that has drawn `End Phase` once
    // can recognise it next turn. The numbers themselves are allocated per
    // session and must never be compared across one -- see the remarks on
    // `Affordance.Id`.
    /// <summary>Who put the pending question, which decides who takes its answer.</summary>
    /// <remarks>
    /// Not derivable from the prompt. A turn option and a step's own question
    /// can both be a <c>Question.TurnOption</c> asked of the same player in the
    /// same phase, and only one of them is answered by
    /// <see cref="Sequence.Answer"/>. Guessing from the agenda's state instead
    /// was wrong in the other direction: a turn prompt is put while the agenda
    /// still has steps left on it, so "the agenda has a current step" is not
    /// "the agenda asked this".
    /// </remarks>
    internal enum Asker
    {
        /// <summary>This class built the prompt — a turn option, a mulligan, an end phase.</summary>
        Game,

        /// <summary>A step or a window asked, through <see cref="Sequence.Work"/>.</summary>
        Sequence,
    }

    internal readonly Dictionary<(string Verb, int Anchor), int> handles = [];
    internal readonly World world;
    internal readonly ICardFacts facts;
    internal readonly Queue<Card> playerSetup = [];
    internal readonly HashSet<int> finishedTurns = [];
    internal readonly HashSet<(
        int Card, int Incarnation, string Face, AbilityType Type, int Ordinal)>
        satisfiedForcedActions = [];

    internal int nextHandle;

    internal Asker asking = Asker.Game;
    internal bool endingPlayerPhase;

    private Game(World world, ICardFacts facts)
    {
        this.world = world;
        this.facts = facts;
        Phase = GamePhase.Mulligan;
        Active = world.FirstPlayer;
        Round = 0;
        asking = Asker.Game;
        if (world.IsOver)
        {
            Phase = GamePhase.Over;
            Pending = null;
        }
        else if (world.ScenarioSetup?.Pending is { } setupPrompt)
        {
            Phase = GamePhase.ScenarioSetup;
            Pending = setupPrompt;
            Active = setupPrompt.Player;
            asking = Asker.Sequence;
        }
        else
        {
            Pending = this.MulliganPrompt();
        }
    }

    /// <summary>The verbs this resolve derives from state alone.</summary>
    public static ImmutableArray<string> DerivedVerbs => Derived;

    /// <summary>The world. The engine's first argument.</summary>
    public World State => world;

    /// <summary>Where the game is in the round structure.</summary>
    public GamePhase Phase { get; internal set; }

    /// <summary>The round number, from 1. Zero before round one begins.</summary>
    public int Round { get; internal set; }

    /// <summary>Whose decision is open.</summary>
    public int Active { get; internal set; }

    /// <summary>The open decision, or <c>null</c> when the game is over.</summary>
    public Prompt? Pending { get; internal set; }

    /// <summary>Whether the pending prompt begins a root game operation.</summary>
    /// <remarks>
    /// Persistence uses this engine-owned distinction to keep dependent
    /// payment, target and timing answers in the unit that opened them. Prompt
    /// shape cannot provide the answer because a sequence may ask the same
    /// question kind as the root menu.
    /// </remarks>
    public bool IsRootPrompt => Pending is not null && asking == Asker.Game;

    /// <summary>Whether the root prompt is a deferred mandatory resolution.</summary>
    public bool IsForcedResolutionPrompt =>
        Pending is not null && asking == Asker.Game && endingPlayerPhase;

    /// <summary>Projects the open decision into the options one seat may submit.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:player-turn.6</c> lets another player trigger an Action during the
    /// active player's turn. The product represents the accepted request or
    /// offer as that player's Action itself, without a request handshake.
    /// </para>
    /// <para>
    /// That exception is deliberately narrow. <c>rr:player-turn.1-.4</c> make
    /// changing form, ordinary card play, identity basic powers, and ally basic
    /// powers options of the player taking their turn. A non-active seat
    /// therefore receives only its own printed Actions, and receives them only
    /// at the root turn menu rather than inside a dependent question.
    /// </para>
    /// </remarks>
    public Prompt? PromptFor(int player) => GameSeatPrompts.For(this, player);

    /// <summary>Opens a dealt board and asks the first question.</summary>
    /// <param name="world">A world from <see cref="WorldSetup.Deal"/>.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">
    /// What cards do. Production callers supply the public compatibility
    /// composition surface; tests that intentionally omit card text use
    /// <see cref="BeginWithoutCardAbilities"/>.
    /// </param>
    /// <remarks>
    /// Setup is not resolved this way — <see cref="WorldSetup"/> runs it and hands
    /// back a world. So this produces no events, and a client attaching here
    /// gets a board to draw rather than a board being dealt. Emitting setup as
    /// events is worth doing and is not free: it is roughly eighty
    /// <c>CardsCreated</c> and two <c>AreaReordered</c>, and nothing records
    /// them today to check against.
    /// </remarks>
    public static Game Begin(World world, ICardFacts facts, ICardAbilities abilities)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        // Nested procedures use the world's narrow ability capabilities:
        // DamagePlacement.Resolve reaches the card's defeat abilities before
        // the card leaves play, as rr:when-defeated-abilities requires.
        world.Abilities = abilities;
        world.SetupAbilities.ValidateForPlay(world);
        return new Game(world, facts);
    }

    /// <summary>Opens a dealt board whose cards intentionally have no executable text.</summary>
    public static Game BeginWithoutCardAbilities(World world, ICardFacts facts) =>
        Begin(world, facts, new NoCardAbilities());

    /// <summary>Applies one answer and produces the next question.</summary>
    public Resolution Resolve(Decision input) => GameResolution.Resolve(this, input);
}
