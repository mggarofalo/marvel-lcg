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
    private const string MulliganTrigger = "WhenPlayerChooseAbility";
    private const string TurnTrigger = "WhenPlayerInTurn";
    private const string EndPhaseTrigger = "End Turn";

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
    private enum Asker
    {
        /// <summary>This class built the prompt — a turn option, a mulligan, an end phase.</summary>
        Game,

        /// <summary>A step or a window asked, through <see cref="Sequence.Work"/>.</summary>
        Sequence,
    }

    private readonly Dictionary<(string Verb, int Anchor), int> handles = [];
    private readonly World world;
    private readonly ICardFacts facts;
    private readonly ICardAbilities abilities;
    private readonly Queue<Card> playerSetup = [];

    private int nextHandle;

    private Asker asking = Asker.Game;

    private Game(World world, ICardFacts facts, ICardAbilities abilities)
    {
        this.world = world;
        this.facts = facts;
        this.abilities = abilities;
        Phase = GamePhase.Mulligan;
        Active = world.FirstPlayer;
        Round = 0;
        Pending = MulliganPrompt();
        asking = Asker.Game;
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
        // The world carries them too, for `rr:when-defeated-abilities` and its
        // like: a defeat happens deep inside `Damage.Deal`, four calls below
        // anything that was handed an `ICardAbilities`.
        world.Abilities = abilities ?? new NoCardAbilities();
        return new Game(world, facts, world.Abilities);
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

        // A question the sequence asked during a player's own turn is answered
        // the same way one in the villain phase is.
        // `rr:attack-player-ability-type.step.7` and `.step.8` put windows
        // around a character's attack, and a turn that could offer the question
        // and not take the answer would be a turn where no card can speak.
        //
        // **And not only windows.** An activation can begin in a player's own
        // turn — Speed Demon's forced interrupt attacks back the moment it is
        // attacked — and `rr:attack-enemy-activation.step.2` is a step that
        // asks who defends rather than a window that offers an ability. That
        // answer was reaching the verb table below and being told that taking
        // 'Defense' is not implemented, which was true only in the sense that
        // it had nowhere to go: `Sequence.Answer` has handled a step's own
        // question since it was written. MARVEL-246.
        if (Phase is GamePhase.PlayerSetup or GamePhase.PlayerTurn
            && asking == Asker.Sequence)
        {
            var during = new List<GameEvent>();
            Sequence.Answer(world, facts, abilities, Pending, input, during);
            return Phase == GamePhase.PlayerSetup
                ? ContinuePlayerSetup(during)
                : Turn(during);
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

            if (Phase == GamePhase.Mulligan
                && string.Equals(verb, ResolveMulligans, StringComparison.Ordinal))
            {
                return Mulligan(input);
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
                // Declining a mulligan keeps the opening hand, so nothing moves
                // -- which is `Mulligan` with an empty list, the same way
                // declining the end-of-phase discard is.
                //
                // `Active` is read from the board here and not left as it was
                // set at `Begin`: the first player is whoever holds the token
                // when the phase starts, and a scenario or a card can move it
                // between the deal and the first turn. Every later round does
                // the same in `Work`.
                return Mulligan(input);

            case GamePhase.PlayerSetup:
                throw new RulesNotImplementedException(
                    "a player Setup ability can only be answered through its agenda question");

            case GamePhase.PlayerTurn:
                // Declining the main turn ends it. Progress in the game's terms
                // and no change to the board -- much the largest class of no-op
                // decision there is, at 187 of 320 declines in the sample this
                // was designed against.
                //
                // `rr:player-phase`: "during the player phase, **each player**
                // *(in player order)* takes one turn". So the phase is over
                // when the last of them has had theirs, not when the first has.
                if (Next(Active) is { } player)
                {
                    Active = player;
                    Pending = TurnPrompt();
        asking = Asker.Game;
                    return new Resolution(world, Pending, []);
                }

                Active = world.FirstPlayer;
                Phase = GamePhase.EndPhase;
                Pending = EndPhasePrompt();
        asking = Asker.Game;
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

        string was = Forms.ChangeAndSchedule(world, seat, facts, Round);
        seat.FormChangedInRound = Round;

        var happened = new List<GameEvent>
        {
            new CardFormChanged(seat.IdentityCard.ObjectId, was, seat.IdentityCard.FaceId)
            {
                // The moment and the action, as every other event carries them.
                // A client is told a card changed face and, without these, not
                // why -- and `rr:player-turn.1` makes changing form one of the
                // six things a turn offers rather than something that merely
                // happens.
                Trigger = TurnTrigger, Verb = ChangeForm,
            },
        };

        return Turn(happened);
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

        return Turn(happened);
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

        // `rr:initiating-abilities.step.5` -- the answer carries which cards
        // were spent, because a cost of resources is a choice of *which* and
        // the affordance already said what could pay.
        var happened = new List<GameEvent>(
            abilities.Act(world, ability, input.Spent, input.Targets));

        return Turn(happened);
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
            input.Spent, happened, input.Targets);

        return Turn(happened);
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
        asking = Asker.Game;
            return new Resolution(world, Pending, happened);
        }

        Phase = GamePhase.VillainPhase;
        world.Agenda.Add(new PhaseStep(Steps.DrawToHandSize, Round, 2));
        world.Agenda.Add(new PhaseStep(Steps.ReadyCards, Round, 3));
        world.Agenda.Add(new PhaseStep(Steps.EndPlayerPhase, Round, 4));
        VillainPhase.Schedule(world.Agenda, Round);
        return Work(happened);
    }

    /// <summary>
    /// Resolves one player's mulligan —
    /// <c>rr:appendix-ii-setup.step.15</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Each player may discard any number of cards from hand, and then draw
    /// up to their starting hand size. <i>(Do not shuffle these discarded
    /// cards back into their decks at this time.)</i>"
    /// </para>
    /// <para>
    /// <b>Discarded, not put back.</b> The parenthesis is the whole of the
    /// difference between this and a deck-bottom mulligan, and it is
    /// observable: the cards are in the discard pile, where
    /// <c>rr:player-deck.4</c> can shuffle them into a new deck later and
    /// where a card that reads a discard pile can see them.
    /// </para>
    /// <para>
    /// <b>Draw up to, not draw that many.</b> A player who discarded three
    /// draws back to their hand size rather than three cards, which is the
    /// same distinction <see cref="PhaseEnd.DrawToHandSize"/> makes.
    /// </para>
    /// <para>
    /// <b>The two readings cannot disagree here, and that is worth knowing
    /// rather than hiding.</b> Step 14 has already drawn every player up to
    /// their hand size, so the hand this is asked about is exactly that size
    /// and "up to hand size" and "as many as went" fetch the same number. A
    /// mutation that swaps one for the other survives every test, and will
    /// until something modifies hand size during setup. The rule's number is
    /// used anyway, because the rule is what this is implementing.
    /// </para>
    /// </remarks>
    /// <param name="input">The answer, carrying the cards to discard.</param>
    private Resolution Mulligan(Decision input)
    {
        var happened = new List<GameEvent>();
        var seat = world.Seats[Active];

        foreach (int id in input.Targets)
        {
            var card = world.Cards[id];
            if (card.Area != seat.Hand)
            {
                throw new RulesNotImplementedException(
                    $"card {id} is not in {seat.Name}'s hand, so it cannot be mulliganed");
            }

            Discard.Card(world, card, MulliganTrigger, happened);
        }

        long limit = PhaseEnd.HandSize(world, seat, facts);
        while (seat.Hand.Cards.Count < limit)
        {
            int before = seat.Hand.Cards.Count;
            Draw.Cards(world, Active, 1, MulliganTrigger, happened);
            if (seat.Hand.Cards.Count == before)
            {
                // `rr:player-deck.4` -- a deck and a discard pile both empty.
                // No card to draw is a legal board, not a stall.
                break;
            }
        }

        if (Next(Active) is { } player)
        {
            Active = player;
            Pending = MulliganPrompt();
            asking = Asker.Game;
            return new Resolution(world, Pending, happened);
        }

        return BeginPlayerSetup(happened);
    }

    /// <summary>
    /// Resolves player-card Setup abilities after every mulligan and before the
    /// first player phase — setup step 16.
    /// </summary>
    private Resolution BeginPlayerSetup(List<GameEvent> happened)
    {
        Phase = GamePhase.PlayerSetup;

        foreach (int player in world.PlayerOrder)
        {
            foreach (var card in abilities.PlayerSetupCards(world, player)
                         .DistinctBy(card => card.ObjectId)
                         .OrderBy(card => card.ObjectId))
            {
                if (!DeckTypes.IsInPlay(card.Area.Type)
                    || card.Area.PlayArea != PlayArea.Of(player))
                {
                    throw new InvalidOperationException(
                        $"card {card.ObjectId} was returned as a Setup card for player "
                        + $"{player}, but it is not in that player's play area");
                }

                playerSetup.Enqueue(card);
            }
        }

        return ContinuePlayerSetup(happened);
    }

    /// <summary>Drains setup work until it needs an answer or round one begins.</summary>
    private Resolution ContinuePlayerSetup(List<GameEvent> happened)
    {
        while (true)
        {
            if (Sequence.Work(world, facts, abilities, happened) is { } asked)
            {
                Active = asked.Player;
                Pending = asked;
                asking = Asker.Sequence;
                return new Resolution(world, Pending, happened);
            }

            if (playerSetup.TryDequeue(out var card))
            {
                happened.AddRange(abilities.Setup(world, card));
                continue;
            }

            Round = 1;
            Active = world.FirstPlayer;
            Phase = GamePhase.PlayerTurn;
            Pending = TurnPrompt();
            asking = Asker.Game;
            return new Resolution(world, Pending, happened);
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
            // `rr:appendix-ii-setup.step.15` gives a player one thing to do
            // and lets them do none of it: "each player **may** discard any
            // number of cards from hand". Taking it with an empty list and
            // declining are the same answer, so a cancel would mean the same
            // thing twice.
            Cancellable: false,
            Affordances: [HandChoice(seat, ResolveMulligans)]);
    }

    /// <summary>
    /// Runs out whatever the turn just put on the agenda, then asks again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A basic attack is <c>Steps.CharacterAttacks</c> and not a call, because
    /// <c>rr:attack-player-ability-type.step.7</c> puts abilities around it and
    /// one of them may ask the player something. So what follows a turn option
    /// is the agenda draining, and the turn prompt is put again only once it
    /// has.
    /// </para>
    /// <para>
    /// <c>rr:player-turn</c> is why the turn prompt comes back at all: "each
    /// option, <b>except 'change form'</b>, may be performed as many times as
    /// the player is able", so a turn is not over because one option was taken.
    /// </para>
    /// </remarks>
    private Resolution Turn(List<GameEvent> happened)
    {
        if (Sequence.Work(world, facts, abilities, happened) is { } asked)
        {
            Pending = asked;
            asking = Asker.Sequence;
            return new Resolution(world, Pending, happened);
        }

        if (world.IsOver)
        {
            // `rr:villain-defeat` -- the players can win in the middle of their
            // own turn, and nothing is asked of anybody after a game is over.
            Phase = GamePhase.Over;
            Pending = null;
            asking = Asker.Game;
            return new Resolution(world, null, happened);
        }

        Pending = TurnPrompt();
        asking = Asker.Game;
        return new Resolution(world, Pending, happened);
    }

    private Resolution Work(List<GameEvent> happened)
    {
        if (Sequence.Work(world, facts, abilities, happened) is { } asked)
        {
            Pending = asked;
            asking = Asker.Sequence;
            return new Resolution(world, Pending, happened);
        }

        if (world.IsOver)
        {
            // The only thing that makes a prompt absent. Nothing is asked of a
            // player after a game is over.
            Phase = GamePhase.Over;
            Pending = null;
            asking = Asker.Game;
            return new Resolution(world, null, happened);
        }

        Round++;
        Phase = GamePhase.PlayerTurn;
        Active = world.FirstPlayer;
        Pending = TurnPrompt();
        asking = Asker.Game;
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
                var hosts = abilities.AttachmentTargets(world, card);
                if (hosts is not { Count: 0 })
                {
                    options.Add(Priced(seat, card, price, hosts));
                }
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
            // **Re-stamped, not taken as given.** `Describe` answers with the
            // card's object id, because a card interpreter has no handle
            // allocator to ask; the rest of this prompt is numbered from a
            // counter. Both are valid handles and they are different number
            // spaces, so an unstamped one collides with a card play sooner or
            // later -- and `Resolve` would then take the play instead of the
            // ability, with nothing anywhere saying so. MARVEL-244.
            var described = abilities.Describe(world, action);
            options.Add(described with
            {
                Verb = ActionVerb,
                Id = Handle(ActionVerb, described.AnchorId),
            });
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
    private Affordance Priced(
        Seat seat, Card card, CostOption price, IReadOnlyList<int>? attachmentTargets)
    {
        int anchor = card.ObjectId;

        return new Affordance(
            Id: Handle(CardPlay.Verb, anchor),
            Verb: CardPlay.Verb,
            AnchorId: anchor,
            AnchorPlayer: seat.Index,
            Label: CardPlay.Verb,

            // The identity, exactly one, and that is measured rather than
            // reasoned: every recorded `Play` affordance carries
            // `targets: {legal: [1], min: 1, max: 1}` where 1 is the identity
            // card. It reads as "into whose play area", which is a real choice
            // at more than one player even though the card is the anchor.
            Targets: attachmentTargets is not null
                ? new TargetRequest(attachmentTargets, Min: 1, Max: 1)
                : new TargetRequest([seat.IdentityCard.ObjectId], Min: 1, Max: 1),
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
            Affordances:
            [
                // `rr:end-of-player-phase.step.1` is two clauses, and the
                // second is a floor: a player "**must** discard down to their
                // hand size if they have more cards than their hand size". So
                // an over-full hand cannot answer with nothing, and the
                // affordance has to say so — `PhaseEnd.DiscardToHandSize`
                // refuses an answer that leaves too many, and an engine that
                // offers what it will refuse has told the client a lie.
                HandChoice(
                    seat,
                    EndPhaseVerb,
                    Math.Max(
                        0,
                        seat.Hand.Cards.Count - (int)PhaseEnd.HandSize(world, seat, facts))),
            ]);
    }

    /// <summary>An affordance offering some number of the player's hand.</summary>
    /// <remarks>
    /// The mulligan and the end phase are nearly the same shape: choose between
    /// <paramref name="least"/> and all of your hand. They differ only in the
    /// floor — <c>rr:appendix-ii-setup.step.15</c> lets a player mulligan "any
    /// number of cards", including none, while the end of the player phase has
    /// a hand size to come down to.
    /// <para>
    /// The candidate list is the hand in its own order, not sorted — the
    /// recorded offer is <c>[42, 45, 37, 9, 47, 46]</c>, which is the hand read
    /// bottom to top, and sorting it would change which card a client
    /// highlights first.
    /// </para>
    /// </remarks>
    private Affordance HandChoice(Seat seat, string verb, int least = 0)
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
                Min: least,
                Max: hand.Length,
                // Looking through your own hand is a search: the cards are
                // hidden from everyone else, so a client presents this as
                // opening the hand rather than as clicking the table.
                IsSearch: true),
        };
    }

    private Affordance Anchored(string verb, Seat seat) =>
        Anchored(verb, seat.IdentityCard, seat);

    /// <summary>
    /// The stable handle for one option, allocating it the first time.
    /// </summary>
    /// <remarks>
    /// <b>Every affordance in a prompt this class builds comes through here</b>,
    /// and that is the point rather than tidiness. <c>Affordance.Id</c> is what
    /// <see cref="Resolve"/> looks the answer up by, and it looks it up with
    /// <c>First</c> — so two options sharing an id in one prompt do not fail,
    /// they silently resolve the wrong one. An ability's own affordance arrives
    /// carrying the card's object id (<c>ICardAbilities.Describe</c> has no
    /// allocator to ask), and a card play carries a counter, and the two number
    /// spaces overlap. MARVEL-244.
    /// </remarks>
    /// <param name="verb">What kind of option it is.</param>
    /// <param name="anchor">The board object it hangs on.</param>
    private int Handle(string verb, int anchor)
    {
        if (!handles.TryGetValue((verb, anchor), out int id))
        {
            id = nextHandle++;
            handles[(verb, anchor)] = id;
        }

        return id;
    }

    /// <summary>An affordance anchored to a particular card.</summary>
    private Affordance Anchored(string verb, Card on, Seat seat)
    {
        int anchor = on.ObjectId;

        return new Affordance(
            Id: Handle(verb, anchor),
            Verb: verb,
            AnchorId: anchor,
            AnchorPlayer: seat.Index,
            // Verb and label are the same string for a derived affordance.
            // There is no second source to fill `Label` from, and filling it
            // from anywhere else would be inventing it -- a client that wants
            // richer wording can build it from the verb and the anchor.
            Label: verb);
    }
}
