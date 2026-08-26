using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

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
/// the set this builds from state alone. The recorded prompts also offer
/// <c>Play</c>, which needs a card's cost, its play restrictions and the
/// resources every other card can generate — card abilities, in other words, and
/// the next piece of work rather than this one. A port checks its coverage
/// against <c>datasets/digest/prompts.json</c> and this set together; neither
/// alone says what is missing.
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

    /// <summary>The verbs this resolve derives from state alone.</summary>
    public static IReadOnlySet<string> DerivedVerbs => Derived;

    /// <summary>The world. The engine's first argument.</summary>
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
    /// Setup is not resolved this way — <see cref="WorldSetup"/> runs it and hands
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
    public Resolution Resolve(Decision input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (Pending is null)
        {
            throw new InvalidOperationException("the game is over; there is nothing to answer");
        }

        if (!input.IsDecline && Phase != GamePhase.VillainPhase)
        {
            // The turn prompts offer things that have to *do* something and
            // most of them are still not written. Naming the verb rather than
            // saying "not implemented" is the difference between a one-line
            // diagnosis and a debugging session.
            //
            // Two exceptions. The villain phase offers an ability waiting in a
            // window or a character declared as a defender, and both are
            // implemented. `Change_Form` is the other, and it is below.
            string verb = Pending.Affordances
                .FirstOrDefault(affordance => affordance.Id == input.Affordance)?.Verb
                ?? $"affordance {input.Affordance}";

            if (string.Equals(verb, ChangeForm, StringComparison.Ordinal))
            {
                return ChangeFormNow();
            }

            if (Phase == GamePhase.PlayerTurn && BasicPower(verb, input) is { } used)
            {
                return used;
            }

            if (Phase == GamePhase.PlayerTurn
                && string.Equals(verb, CardPlay.Verb, StringComparison.Ordinal))
            {
                return PlayCard(input);
            }

            if (Phase == GamePhase.PlayerTurn
                && string.Equals(verb, ActionVerb, StringComparison.Ordinal))
            {
                return TriggerAction(input);
            }

            if (string.Equals(verb, EndPhaseVerb, StringComparison.Ordinal)
                && Phase == GamePhase.EndPhase)
            {
                // `rr:end-of-player-phase.step.1`. Taking this is discarding
                // the cards named in the answer; declining is discarding none,
                // which is the same code path with an empty list.
                return EndOfPlayerPhase(input);
            }

            throw new RulesNotImplementedException(
                $"taking '{verb}' is not implemented; this resolve only declines");
        }

        switch (Phase)
        {
            case GamePhase.Mulligan:
                // Declining a mulligan keeps the opening hand, so nothing moves.
                //
                // `Active` is read from the board here and not left as it was
                // set at `Begin`: the first player is whoever holds the token
                // when the phase starts, and a scenario or a card can move it
                // between the deal and the first turn. Every later round does
                // the same in `Work`.
                Round = 1;
                Active = world.FirstPlayer;
                Phase = GamePhase.PlayerTurn;
                Pending = TurnPrompt();
                return new Resolution(world, Pending, []);

            case GamePhase.PlayerTurn:
                // Declining the main turn ends it. Progress in the game's terms
                // and no change to the board -- the largest class of no-op
                // decision in the corpus at 187 of 320 declines.
                //
                // `rr:player-phase`: "during the player phase, **each player**
                // *(in player order)* takes one turn". So the phase is over
                // when the last of them has had theirs, not when the first has.
                if (Next(Active) is { } player)
                {
                    Active = player;
                    Pending = TurnPrompt();
                    return new Resolution(world, Pending, []);
                }

                Active = world.FirstPlayer;
                Phase = GamePhase.EndPhase;
                Pending = EndPhasePrompt();
                return new Resolution(world, Pending, []);

            case GamePhase.EndPhase:
                return EndOfPlayerPhase(input);

            case GamePhase.VillainPhase:
                // Answering a question the villain phase asked. The window
                // absorbs the answer and the agenda carries on from where it
                // stopped.
                return Work(Answer(input));

            default:
                throw new RulesNotImplementedException($"the {Phase} phase is not implemented");
        }
    }

    /// <summary>
    /// Flip the active player's identity, and ask them again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:form-change-form.1</c>: "Once each round, during their turn, each
    /// player is permitted to change form by flipping their identity card." All
    /// three qualifications are load-bearing and all three are enforced here —
    /// once, each round, during their turn.
    /// </para>
    /// <para>
    /// The turn does <b>not</b> end. Changing form is one thing a player may do
    /// in their turn rather than the whole of it, so the same prompt is put
    /// again — this time without the option, because it has been used.
    /// </para>
    /// <para>
    /// <c>rr:form-change-form.3</c> is why this counter lives on the seat and
    /// not inside <see cref="Forms.Change"/>: "if a card ability causes a player
    /// to change forms, it does not count against the one voluntary form change
    /// the player is permitted". An ability calls the flip without touching the
    /// count, so the count belongs to the permission and not to the flip.
    /// </para>
    /// </remarks>
    private Resolution ChangeFormNow()
    {
        var seat = world.Seats[Active];
        if (seat.FormChangedInRound == Round)
        {
            throw new RulesNotImplementedException(
                $"'{seat.Name}' has already changed form in round {Round}, and "
                + "rr:form-change-form.1 permits one voluntary change each round");
        }

        string was = Forms.Change(seat, facts);
        seat.FormChangedInRound = Round;

        var happened = new List<GameEvent>
        {
            new CardFormChanged(seat.IdentityCard.ObjectId, was, seat.IdentityCard.FaceId),
        };

        Pending = TurnPrompt();
        return new Resolution(world, Pending, happened);
    }

    /// <summary>
    /// Uses a basic power, or answers null if that verb is not one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:player-turn</c>: "each option, <b>except 'change form'</b>, may be
    /// performed as many times as the player is able" — so the turn does not
    /// end here. The same prompt is put again, and what it offers the second
    /// time is whatever is still possible: after a basic attack the character
    /// is exhausted, so nothing is.
    /// </para>
    /// <para>
    /// The target comes off <see cref="Decision.Targets"/> rather than being
    /// chosen here. <c>rr:initiating-abilities</c> separates choosing a target
    /// from paying for it and from resolving, and the affordance already said
    /// which targets were legal.
    /// </para>
    /// </remarks>
    /// <param name="verb">The affordance's verb.</param>
    /// <param name="input">The answer, carrying the target.</param>
    private Resolution? BasicPower(string verb, Decision input)
    {
        var happened = new List<GameEvent>();

        // Which character is using the power is the affordance's anchor: the
        // identity for a hero's own, the ally for `rr:player-turn.4`. Reading
        // it here rather than switching on the verb is what keeps the two from
        // needing separate verbs -- the recording spells an ally's attack
        // `Attack`, the same as a hero's.
        var user = Pending!.Affordances
            .FirstOrDefault(option => option.Id == input.Affordance) is { } taken
            ? world.Cards[taken.AnchorId]
            : world.Seats[Active].IdentityCard;

        bool byAlly = facts.Kind(user.FaceId) == CardKind.Ally;
        switch (verb)
        {
            case BasicPowers.AttackVerb when byAlly:
            case BasicPowers.ThwartVerb when byAlly:
                BasicPowers.AllyPower(
                    world, facts, user, world.Cards[Only(input, verb)], verb, happened);
                break;

            case BasicPowers.AttackVerb:
                BasicPowers.BasicAttack(
                    world, facts, Active, world.Cards[Only(input, verb)], happened);
                break;

            case BasicPowers.ThwartVerb:
                BasicPowers.BasicThwart(
                    world, facts, Active, world.Cards[Only(input, verb)], happened);
                break;

            case BasicPowers.RecoverVerb:
                BasicPowers.BasicRecovery(world, facts, Active, happened);
                break;

            default:
                return null;
        }

        if (world.IsOver)
        {
            // `rr:villain-defeat` -- the players can win here, and nothing is
            // asked of anybody after a game is over.
            Phase = GamePhase.Over;
            Pending = null;
            return new Resolution(world, null, happened);
        }

        Pending = TurnPrompt();
        return new Resolution(world, Pending, happened);
    }

    /// <summary>
    /// Triggers the "Action" ability an affordance is anchored to —
    /// <c>rr:player-turn.5</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Like a basic power, the turn does not end: <c>rr:player-turn</c> lets
    /// every option except changing form "be performed as many times as the
    /// player is able", so what is put again is the turn prompt with whatever
    /// is still possible.
    /// </para>
    /// <para>
    /// The ability is found again from the card rather than carried on the
    /// affordance, for the same reason a suspended choice is: an affordance is
    /// a small value on the wire and an ability is a tree.
    /// </para>
    /// </remarks>
    private Resolution TriggerAction(Decision input)
    {
        var taken = Pending!.Affordances.First(option => option.Id == input.Affordance);
        var ability = abilities.Actions(world, Active)
            .FirstOrDefault(pending => pending.Card == taken.AnchorId);

        if (ability.Card != taken.AnchorId)
        {
            throw new RulesNotImplementedException(
                $"card {taken.AnchorId} has no action this player can trigger");
        }

        // `rr:action` is not a window, so there is no occurrence around it --
        // the ability *is* what is happening, and the card it is on is its
        // subject.
        var happened = new List<GameEvent>(abilities.Resolve(
            world,
            new Occurrence(0, [Steps.TurnAction], Subject: taken.AnchorId, Player: Active),
            ability));

        if (world.IsOver)
        {
            Phase = GamePhase.Over;
            Pending = null;
            return new Resolution(world, null, happened);
        }

        Pending = TurnPrompt();
        return new Resolution(world, Pending, happened);
    }

    /// <summary>
    /// Plays the card an affordance is anchored to.
    /// </summary>
    /// <remarks>
    /// The card is the affordance's anchor and the payment is
    /// <see cref="Decision.Spent"/> — <c>rr:initiating-abilities</c> keeps
    /// choosing a card, determining its cost and paying that cost in separate
    /// steps, and the answer carries the last of them. Like a basic power, the
    /// turn does not end.
    /// </remarks>
    /// <param name="input">The answer, carrying the resources spent.</param>
    private Resolution PlayCard(Decision input)
    {
        var seat = world.Seats[Active];
        var affordance = Pending!.Affordances.First(
            option => option.Id == input.Affordance);

        var happened = new List<GameEvent>();
        CardPlay.Play(
            world, facts, abilities, seat, world.Cards[affordance.AnchorId],
            input.Spent, happened);

        Pending = TurnPrompt();
        return new Resolution(world, Pending, happened);
    }

    /// <summary>The one target a basic power takes.</summary>
    private static int Only(Decision input, string verb) =>
        input.Targets.Count == 1
            ? input.Targets[0]
            : throw new RulesNotImplementedException(
                $"a basic {verb} takes exactly one target and was given "
                + $"{input.Targets.Count}");

    /// <summary>
    /// The seat after this one in player order, or null at the end of the table.
    /// </summary>
    /// <remarks>
    /// <c>rr:in-player-order.2</c>: "the phrase 'next player' always refers to
    /// the next <i>(clockwise)</i> player in player order." Null rather than
    /// wrapping, because both callers want to know when the round of
    /// opportunities is <i>complete</i> — <c>rr:in-player-order.1</c>'s
    /// condition for stopping.
    /// </remarks>
    /// <param name="seat">The seat that has just finished.</param>
    private int? Next(int seat)
    {
        int taken = ((seat - world.FirstPlayer) + world.Players) % world.Players;
        return taken + 1 < world.Players
            ? (world.FirstPlayer + taken + 1) % world.Players
            : null;
    }

    /// <summary>
    /// Steps 1 to 5 of <c>rr:end-of-player-phase</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Step 1 is this prompt: "in player order, each player may discard any
    /// number of cards from their hand". <b>In player order</b>, so it is asked
    /// once per seat and the phase does not move on until the last has
    /// answered. Steps 2 and 3 are "simultaneously", so they are one step each
    /// on the agenda rather than one per player.
    /// </para>
    /// <para>
    /// Declining discards nothing, which the rule allows — "<b>may</b> discard
    /// any number" — right up until the hand is over its size, and
    /// <see cref="PhaseEnd.DiscardToHandSize"/> is where that is refused.
    /// </para>
    /// </remarks>
    /// <param name="input">The answer: which cards this player discards.</param>
    private Resolution EndOfPlayerPhase(Decision input)
    {
        var happened = new List<GameEvent>();
        PhaseEnd.DiscardToHandSize(world, facts, Active, input.Targets, happened);

        if (Next(Active) is { } player)
        {
            Active = player;
            Pending = EndPhasePrompt();
            return new Resolution(world, Pending, happened);
        }

        Phase = GamePhase.VillainPhase;
        world.Agenda.Add(new PhaseStep(Steps.DrawToHandSize, Round, 2));
        world.Agenda.Add(new PhaseStep(Steps.ReadyCards, Round, 3));
        world.Agenda.Add(new PhaseStep(Steps.EndPlayerPhase, Round, 4));
        VillainPhase.Schedule(world.Agenda, Round);
        return Work(happened);
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

    private Resolution Work(List<GameEvent> happened)
    {
        if (Sequence.Work(world, facts, abilities, happened) is { } asked)
        {
            Pending = asked;
            return new Resolution(world, Pending, happened);
        }

        if (world.IsOver)
        {
            // The only thing that makes a prompt absent. Nothing is asked of a
            // player after a game is over.
            Phase = GamePhase.Over;
            Pending = null;
            return new Resolution(world, null, happened);
        }

        Round++;
        Phase = GamePhase.PlayerTurn;
        Active = world.FirstPlayer;
        Pending = TurnPrompt();
        return new Resolution(world, Pending, happened);
    }

    private List<GameEvent> Answer(Decision input)
    {
        var happened = new List<GameEvent>();
        if (Pending is { } asked)
        {
            Sequence.Answer(world, facts, abilities, asked, input, happened);
        }

        return happened;
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
            Affordances: TurnOptions(seat));
    }

    /// <summary>
    /// What a player may do on their turn — <c>rr:player-turn</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only what is offered <i>and</i> can be taken. An affordance that would
    /// throw when taken is worse than an absent one — MARVEL-130 is that same
    /// defect on the action menu.
    /// </para>
    /// <para>
    /// <c>rr:player-turn</c> lists six options and five are here: change form,
    /// playing a card, ally actions, the basic powers, and triggering an
    /// action. Asking another player (<c>.6</c>) is not written.
    /// </para>
    /// </remarks>
    /// <param name="seat">Whose turn.</param>
    private List<Affordance> TurnOptions(Seat seat)
    {
        var options = new List<Affordance>();

        // `rr:form-change-form.1` permits one voluntary change each round, so a
        // player who has used theirs is not offered it again.
        if (seat.FormChangedInRound != Round)
        {
            options.Add(Anchored(ChangeForm, seat));
        }

        // `rr:player-turn.2`: "play an ally, upgrade, support, or player side
        // scheme card from hand". Priced per card, and a card that cannot be
        // paid for is not offered -- `rr:initiating-abilities.step.3` checks
        // "the player's ability to pay them" before anything is spent.
        // By object id, which is the order the recorded prompt lists them --
        // `Play@37, 45, 46, 47` against a hand held in the order
        // `42, 45, 37, 9, 47, 46`. Measured on one board, so it is the simplest
        // reading that fits rather than a rule anything states.
        foreach (var card in seat.Hand.Cards.OrderBy(card => card.ObjectId))
        {
            if (CardPlay.Price(world, facts, seat, card) is { } price)
            {
                options.Add(Priced(seat, card, price));
            }
        }

        // `rr:player-turn.4`: "use an ally card they control in play to attack
        // an enemy or thwart a scheme". `rr:ally.5` puts these outside the
        // identity -- "attacks [...] that resolve from allies in play under a
        // player's control are **not** considered to be performed by that
        // player's identity" -- so they are offered whatever form the player is
        // in, and whether the identity is exhausted does not matter.
        foreach (var ally in BasicPowers.Allies(world, seat.Index))
        {
            Offer(options, ally, BasicPowers.AttackVerb,
                BasicPowers.Attackable(world, facts, seat.Index));
            Offer(options, ally, BasicPowers.ThwartVerb,
                BasicPowers.Thwartable(world, facts, seat.Index));
        }

        // `rr:player-turn.5`: "trigger an **Action** ability on a card in play
        // they control, an encounter card in play, [...] or an event card in
        // their hand (by playing that event)". Not a window -- an action is one
        // of the six things a turn offers, so it is asked with the others.
        //
        // `.5.1` is applied where the ability is found: "if the action ability
        // is preceded by Hero or Alter-Ego, the player must be in the specified
        // form", and 728 of the 966 in the pool are.
        foreach (var action in abilities.Actions(world, seat.Index))
        {
            options.Add(abilities.Describe(world, action) with { Verb = ActionVerb });
        }

        // `rr:player-turn.3`: the hero's basic attack or thwart in hero form,
        // the alter-ego's basic recovery in alter-ego form. A character that is
        // exhausted cannot pay the cost of any of them (`rr:exhausted.2`).
        if (!seat.IdentityCard.Ready)
        {
            return options;
        }

        if (Forms.In(world, seat, facts, Forms.Hero))
        {
            // `rr:attack-player-ability-type.1.1` and `rr:thwart.1.1`: a basic
            // attack needs an enemy that can be attacked and a basic thwart
            // needs a scheme with at least one threat on it. With neither, the
            // power is not on offer at all.
            Offer(options, seat.IdentityCard, BasicPowers.AttackVerb,
                BasicPowers.Attackable(world, facts, seat.Index));
            Offer(options, seat.IdentityCard, BasicPowers.ThwartVerb,
                BasicPowers.Thwartable(world, facts, seat.Index));
        }
        else if (BasicPowers.CanRecover(world, facts, seat.Index))
        {
            options.Add(Anchored(BasicPowers.RecoverVerb, seat));
        }

        return options;
    }

    /// <summary>Offers one card in hand, anchored to the card rather than the seat.</summary>
    /// <remarks>
    /// A play is clicked on the card, not on the identity, so this is the one
    /// affordance whose anchor is not <see cref="Seat.IdentityCard"/>. The
    /// handle is cached on <c>(verb, anchor)</c> like any other, which gives a
    /// card in hand a stable id across the re-offers of one turn.
    /// </remarks>
    private Affordance Priced(Seat seat, Card card, CostOption price)
    {
        int anchor = card.ObjectId;
        if (!handles.TryGetValue((CardPlay.Verb, anchor), out int id))
        {
            id = nextHandle++;
            handles[(CardPlay.Verb, anchor)] = id;
        }

        return new Affordance(
            Id: id,
            Verb: CardPlay.Verb,
            AnchorId: anchor,
            AnchorPlayer: seat.Index,
            Label: CardPlay.Verb,

            // The identity, exactly one, and that is measured rather than
            // reasoned: every recorded `Play` affordance carries
            // `targets: {legal: [1], min: 1, max: 1}` where 1 is the identity
            // card. It reads as "into whose play area", which is a real choice
            // at more than one player even though the card is the anchor.
            Targets: new TargetRequest([seat.IdentityCard.ObjectId], Min: 1, Max: 1),
            Costs: [price]);
    }

    /// <summary>Offers a targeted basic power, if it has a legal target.</summary>
    /// <remarks>
    /// Anchored to the character using it, which is the identity for a hero's
    /// own power and the ally for <c>rr:player-turn.4</c>. Two allies attacking
    /// are two options, because <c>rr:ally.2</c> permits "any number".
    /// </remarks>
    private void Offer(
        List<Affordance> options, Card character, string verb, IReadOnlyList<Card> targets)
    {
        if (targets.Count == 0)
        {
            return;
        }

        options.Add(Anchored(verb, character, world.Seats[Active]) with
        {
            // Exactly one target: `rr:attack-player-ability-type.1` and
            // `rr:thwart.1` are each one enemy or one scheme. An ability that
            // hits several is a different thing (`.5`) and is not a basic power.
            Targets = new TargetRequest(
                [.. targets.Select(target => target.ObjectId)], Min: 1, Max: 1),
        });
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

    private Affordance Anchored(string verb, Seat seat) =>
        Anchored(verb, seat.IdentityCard, seat);

    /// <summary>An affordance anchored to a particular card.</summary>
    private Affordance Anchored(string verb, Card on, Seat seat)
    {
        int anchor = on.ObjectId;
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
