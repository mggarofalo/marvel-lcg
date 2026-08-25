using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Fold;

/// <summary>Where a game is in the round structure.</summary>
/// <remarks>
/// The Python engine's console log names each of these as it enters it, which is
/// how the sequence was read: <c>=== Round 1 Start ===</c>,
/// <c>--- Player Phase ---</c>, <c>--- Spider-Man's Turn (1) ---</c>,
/// <c>--- Spider-Man Turn End ---</c>, <c>Spider-Man End Phase</c>,
/// <c>--- Player Phase End ---</c>, <c>--- Villain Phase ---</c>.
/// </remarks>
public enum GamePhase
{
    /// <summary>Before round one. Each player may mulligan their opening hand.</summary>
    Mulligan,

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
/// The fold: a world, where it is in the round, and what it will ask next.
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
/// the set this builds from state alone. The recorded prompts also offer
/// <c>Play</c>, which needs a card's cost, its play restrictions and the
/// resources every other card can generate — card abilities, in other words, and
/// the next piece of work rather than this one. A port checks its coverage
/// against <c>datasets/digest/prompts.json</c> and this set together; neither
/// alone says what is missing.
/// </para>
/// <para>
/// <b>Not a hot path yet.</b> <c>docs/presentation-layer.md</c> asks for LINQ out
/// of the fold and a flat array of cards. The flat array is already how
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

    /// <summary>The affordance verb for resolving a turn's end phase.</summary>
    public const string EndPhaseVerb = "End Phase";

    // The engine's `message_name` at each of the three prompts. `End Turn` is
    // not spelled like the others and that is not a transcription slip -- the
    // other two are timing points and it is a message name. Recorded as
    // measured rather than regularised.
    private const string MulliganTrigger = "WhenPlayerChooseAbility";
    private const string TurnTrigger = "WhenPlayerInTurn";
    private const string EndPhaseTrigger = "End Turn";

    private static readonly HashSet<string> Derived =
        [ResolveMulligans, ChangeForm, EndPhaseVerb];

    // An affordance id is a handle. The Python engine hands out effect object
    // ids, which are stable for the life of a game -- `End Phase` is id 1 at
    // recorded steps 2, 4 and 6, and `Change_Form` is id 7 at steps 1, 3 and 5.
    // This reproduces that property without reproducing the numbers: the same
    // option re-offered keeps its handle. The numbers themselves are not
    // reproduced and must not be compared, because effect ids are allocated per
    // session and drift -- see the remarks on `Affordance.Id`.
    private readonly Dictionary<(string Verb, int Anchor), int> handles = [];
    private readonly World world;
    private readonly ICardFacts facts;
    private readonly ICardAbilities abilities;

    private int nextHandle;

    private Game(World world, ICardFacts facts, ICardAbilities abilities)
    {
        this.world = world;
        this.facts = facts;
        this.abilities = abilities;
        Phase = GamePhase.Mulligan;
        Active = world.FirstPlayer;
        Round = 0;
        Pending = MulliganPrompt();
    }

    /// <summary>The verbs this fold derives from state alone.</summary>
    public static IReadOnlySet<string> DerivedVerbs => Derived;

    /// <summary>The world. The fold's first argument.</summary>
    public World State => world;

    /// <summary>Where the game is in the round structure.</summary>
    public GamePhase Phase { get; private set; }

    /// <summary>The round number, from 1. Zero before round one begins.</summary>
    public int Round { get; private set; }

    /// <summary>Whose decision is open.</summary>
    public int Active { get; private set; }

    /// <summary>The open decision, or <c>null</c> when the game is over.</summary>
    public Prompt? Pending { get; private set; }

    /// <summary>Opens a dealt board and asks the first question.</summary>
    /// <param name="world">A world from <see cref="WorldSetup.Deal"/>.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">
    /// What revealed cards do. Defaults to <see cref="NoCardAbilities"/>, which
    /// is an engine with no cards ported — every villain phase then places
    /// threat and discards correctly and no card's own text ever fires.
    /// </param>
    /// <remarks>
    /// Setup itself is not folded — <see cref="WorldSetup"/> runs it and hands
    /// back a world. So this produces no events, and a client attaching here
    /// gets a board to draw rather than a board being dealt. Emitting setup as
    /// events is worth doing and is not free: it is roughly eighty
    /// <c>CardsCreated</c> and two <c>AreaReordered</c>, and nothing records
    /// them today to check against.
    /// </remarks>
    public static Game Begin(World world, ICardFacts facts, ICardAbilities? abilities = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        return new Game(world, facts, abilities ?? new NoCardAbilities());
    }

    /// <summary>Applies one answer and produces the next question.</summary>
    /// <param name="input">The answer. <see cref="Decision.Decline"/> takes nothing.</param>
    /// <exception cref="RulesNotImplementedException">
    /// The answer, or the phase it leads to, needs a rule this engine does not
    /// have. Thrown before the world is touched.
    /// </exception>
    /// <exception cref="InvalidOperationException">The game is already over.</exception>
    public FoldResult Fold(Decision input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (Pending is null)
        {
            throw new InvalidOperationException("the game is over; there is nothing to answer");
        }

        if (!input.IsDecline)
        {
            // Every affordance this fold offers is one that has to *do*
            // something, and none of them are written. Naming the verb rather
            // than saying "not implemented" is the difference between a
            // one-line diagnosis and a debugging session.
            string verb = Pending.Affordances
                .FirstOrDefault(affordance => affordance.Id == input.Affordance)?.Verb
                ?? $"affordance {input.Affordance}";
            throw new RulesNotImplementedException(
                $"taking '{verb}' is not implemented; this fold only declines");
        }

        switch (Phase)
        {
            case GamePhase.Mulligan:
                // Declining a mulligan keeps the opening hand, so nothing moves.
                Round = 1;
                Phase = GamePhase.PlayerTurn;
                Pending = TurnPrompt();
                return new FoldResult(world, Pending, []);

            case GamePhase.PlayerTurn:
                // Declining the main turn ends it. Progress in the game's terms
                // and no change to the board -- the largest class of no-op
                // decision in the corpus at 187 of 320 declines.
                Phase = GamePhase.EndPhase;
                Pending = EndPhasePrompt();
                return new FoldResult(world, Pending, []);

            case GamePhase.EndPhase:
                // The end phase refills the hand to hand size, and this game
                // still cannot say when: the recorded hand is full at every
                // step, so the trace is identical whether the refill happens
                // before this prompt or after it. Left out rather than guessed.
                Phase = GamePhase.VillainPhase;
                var happened = new List<GameEvent>();

                // rr:end-of-player-phase.step.4 and .step.5. Steps 1 to 3 --
                // discard down to hand size, draw up to it, ready every card --
                // are not implemented; see PhaseEnd.EndPlayerPhase.
                PhaseEnd.EndPlayerPhase(world, abilities, Round, happened);

                happened.AddRange(VillainPhase.Run(world, facts, abilities, Round));

                if (world.IsOver)
                {
                    // The only thing that makes a prompt absent. Nothing is
                    // asked of a player after a game is over.
                    Phase = GamePhase.Over;
                    Pending = null;
                    return new FoldResult(world, null, happened);
                }

                Round++;
                Phase = GamePhase.PlayerTurn;
                Active = world.FirstPlayer;
                Pending = TurnPrompt();
                return new FoldResult(world, Pending, happened);

            default:
                throw new RulesNotImplementedException($"the {Phase} phase is not implemented");
        }
    }

    private Prompt MulliganPrompt()
    {
        var seat = world.Seats[Active];
        return new Prompt(
            Player: seat.Index,
            Asking: Question.TurnOption,
            When: Timing.TimingPriority.Untimed,
            Trigger: MulliganTrigger,
            Label: $"{seat.Name} resolves mulligans",
            // The engine asks this forced. There is no "keep my hand" option to
            // take -- declining is how you keep it -- so a cancel would mean
            // the same thing twice.
            Cancellable: false,
            Affordances: [HandChoice(seat, ResolveMulligans)]);
    }

    private Prompt TurnPrompt()
    {
        var seat = world.Seats[Active];
        return new Prompt(
            Player: seat.Index,
            Asking: Question.TurnOption,
            When: Timing.TimingPriority.Untimed,
            Trigger: TurnTrigger,
            // The engine's console line, newline and all. Normalising it is how
            // two implementations quietly stop agreeing about a string that is
            // on the wire.
            Label: $"\n--- {seat.Name}'s Turn ({Round}) ---",
            Cancellable: true,
            Affordances: [Anchored(ChangeForm, seat)]);
    }

    private Prompt EndPhasePrompt()
    {
        var seat = world.Seats[Active];
        return new Prompt(
            Player: seat.Index,
            Asking: Question.TurnOption,
            When: Timing.TimingPriority.Untimed,
            Trigger: EndPhaseTrigger,
            Label: $"{seat.Name} End Phase",
            Cancellable: false,
            Affordances: [HandChoice(seat, EndPhaseVerb)]);
    }

    /// <summary>An affordance offering any number of the player's hand.</summary>
    /// <remarks>
    /// The mulligan and the end phase are the same shape: choose between none
    /// and all of your hand. The candidate list is the hand in its own order,
    /// not sorted — the recorded offer is <c>[42, 45, 37, 9, 47, 46]</c>, which
    /// is the hand read bottom to top, and sorting it would change which card a
    /// client highlights first.
    /// </remarks>
    private Affordance HandChoice(Seat seat, string verb)
    {
        var hand = new int[seat.Hand.Cards.Count];
        for (int index = 0; index < hand.Length; index++)
        {
            hand[index] = seat.Hand.Cards[index].ObjectId;
        }

        return Anchored(verb, seat) with
        {
            Targets = new TargetRequest(
                Legal: hand,
                Min: 0,
                Max: hand.Length,
                // Looking through your own hand is a search: the cards are
                // hidden from everyone else, so a client presents this as
                // opening the hand rather than as clicking the table.
                IsSearch: true),
        };
    }

    private Affordance Anchored(string verb, Seat seat)
    {
        int anchor = seat.IdentityCard.ObjectId;
        if (!handles.TryGetValue((verb, anchor), out int id))
        {
            id = nextHandle++;
            handles[(verb, anchor)] = id;
        }

        return new Affordance(
            Id: id,
            Verb: verb,
            AnchorId: anchor,
            AnchorPlayer: seat.Index,
            // The Python engine carries one string where this record carries
            // two: an option's `name` is both its verb and its label, and there
            // is no second source to fill `Label` from. Filling it from anywhere
            // else would be inventing it.
            Label: verb);
    }
}
