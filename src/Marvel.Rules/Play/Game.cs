using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Where a game is in the round structure.</summary>
/// <remarks>
/// The order is <c>rr:game-round</c>'s: a round is a player phase and then a
/// villain phase, and the player phase is each player's turn in player order,
/// each turn ending with that player's end phase.
/// </remarks>
public enum GamePhase
{
    /// <summary>Before round one. Each player may mulligan their opening hand.</summary>
    Mulligan,

    /// <summary>Player-card Setup abilities resolve before round one.</summary>
    PlayerSetup,

    /// <summary>A player is taking their turn.</summary>
    PlayerTurn,

    /// <summary>That player's turn has ended and they are resolving their end phase.</summary>
    EndPhase,

    /// <summary>Every player has finished. The villain acts.</summary>
    VillainPhase,

    /// <summary>Somebody has won.</summary>
    Over,
}

/// <summary>
/// The engine: a world, where it is in the round, and what it will ask next.
/// </summary>
/// <remarks>
/// <para>
/// <b>What this implements.</b> The player phase for a player who declines. That
/// is three prompt shapes — the mulligan, the main turn, and the end phase — and
/// the transitions between them, which are exactly the transitions the recorded
/// milestone game exercises before it needs a card ability. Measured against
/// <c>rhino / spider_man / 12345</c>: the recording holds seven steps and those
/// seven are three distinct boards, because the board only moves in the villain
/// phase. Steps 0, 1 and 2 are one board and this reproduces all three of them.
/// </para>
/// <para>
/// <b>What it does not implement, and how you find out.</b> Anything else throws
/// <see cref="RulesNotImplementedException"/> before touching the world. Two
/// boundaries exist today: taking an affordance rather than declining one, and
/// the villain phase. Both are named in the message.
/// </para>
/// <para>
/// <b>The affordances are the derivable ones.</b> <see cref="DerivedVerbs"/> is
/// the set this builds from state alone. <c>Play</c> is the other one, and it
/// needs a card's cost, its play restrictions and the resources every other
/// card can generate — card abilities, in other words, which is why it lives in
/// <see cref="CardPlay"/> and not here.
/// </para>
/// <para>
/// <b>Not a hot path yet.</b> <c>docs/presentation-layer.md</c> asks for LINQ out
/// of the engine and a flat array of cards. The flat array is already how
/// <see cref="World"/> stores them; the LINQ here is in prompt construction,
/// which runs once per decision rather than once per effect node, and it will be
/// measured before it is optimised.
/// </para>
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

    private static readonly HashSet<string> Derived =
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
        Pending = this.MulliganPrompt();
        asking = Asker.Game;
    }

    /// <summary>The verbs this resolve derives from state alone.</summary>
    public static IReadOnlySet<string> DerivedVerbs => Derived;

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
    public Prompt? PromptFor(int player)
    {
        if (player < 0 || player >= world.Players)
        {
            throw new ArgumentOutOfRangeException(nameof(player));
        }

        if (Pending is not { } pending || world.Seats[player].Eliminated)
        {
            return null;
        }

        if (!IsRootTurn())
        {
            return pending.Player == player ? pending : null;
        }

        var options = pending.Affordances.Where(option => OfferedTo(option, pending, player)).ToList();
        if (options.Count == 0 && pending.Player != player)
        {
            return null;
        }

        return pending with
        {
            Player = player,
            Label = PromptLabel(pending, player),
            // An off-turn menu is an invitation to act, not a sequential
            // question. Declining it must not end the active player's turn.
            Cancellable = pending.Player == player && pending.Cancellable,
            Affordances = options,
        };
    }

    private bool IsRootTurn() => Phase == GamePhase.PlayerTurn
        && asking == Asker.Game && !endingPlayerPhase;

    private static bool OfferedTo(Affordance option, Prompt pending, int player) =>
        string.Equals(option.Verb, ActionVerb, StringComparison.Ordinal)
            ? option.AnchorPlayer == player : pending.Player == player;

    private string PromptLabel(Prompt pending, int player) => pending.Player == player
        ? pending.Label
        : $"{world.Seats[player].Name} may act during "
            + $"{world.Seats[pending.Player].Name}'s turn";

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
        // The world carries them too, for `rr:when-defeated-abilities` and its
        // like: a defeat happens deep inside `Damage.Deal`, four calls below
        // the public compatibility entry point.
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
