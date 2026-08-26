using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>
/// Runs authored card abilities. The one way a card's text enters the engine.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is what replaces a class per card.</b> There was one — a switch on
/// printed id, three cards deep and growing — and it was the "cards as scripts"
/// inversion this port exists to undo (<c>docs/migration.md</c>). A card is now
/// a row in <c>datasets/abilities/abilities.json</c>, and adding one is
/// authoring data rather than compiling code.
/// </para>
/// <para>
/// The vocabulary is small and every gap is loud. A node nothing implements
/// throws naming the node; a card nobody has authored throws naming the card.
/// Growing the engine means adding a case here and growing the game means adding
/// a row there, and the two are different activities on purpose.
/// </para>
/// <para>
/// See <c>docs/card-dsl.md</c> for the design this is the first executable
/// piece of, and <c>docs/enemy-attacks.md</c> for the cards it currently runs.
/// </para>
/// </remarks>
/// <param name="book">The authored cards.</param>
public sealed class AbilityRunner(AbilityBook book) : ICardAbilities
{
    private readonly AbilityBook book = book;

    /// <summary>The verb an option carries on the wire.</summary>
    public const string ChooseVerb = "Choose_Option";

    private static readonly string[] Branches = ["then", "else"];

    private static readonly DeckType[] Owned = [DeckType.UpgradesArea, DeckType.SupportsArea];

    /// <summary>The authored cards, whether or not they do anything.</summary>
    public IReadOnlySet<string> Authored => book.Authored;

    /// <inheritdoc/>
    public IReadOnlyList<PendingAbility> Waiting(
        World world, Occurrence occurrence, WindowKind window)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);

        var waiting = new List<PendingAbility>();
        foreach (var card in world.Cards)
        {
            // `rr:ability.1` -- a card's ability functions while the card is in
            // play. Being attached to something is not the same as being in
            // play: the recorded Tough hangs off Rhino from a zone that is not.
            if (!DeckTypes.IsInPlay(card.Area.Type))
            {
                continue;
            }

            foreach (var ability in book.On(card.FaceId))
            {
                if (Answers(ability, card, occurrence, window))
                {
                    // The controller is the card's owner rather than anything
                    // the data says: `rr:ability.8` lets any player use an
                    // optional ability on an encounter card, and an encounter
                    // card is one the scenario owns.
                    waiting.Add(new PendingAbility(
                        card.ObjectId, ability.Trigger.Timing, card.Owner));
                }
            }
        }

        return waiting;
    }

    /// <inheritdoc/>
    public Affordance Describe(World world, PendingAbility ability)
    {
        ArgumentNullException.ThrowIfNull(world);

        var card = world.Cards[ability.Card];
        var found = book.On(card.FaceId)
            .FirstOrDefault(candidate => candidate.Trigger.Timing == ability.Type)
            ?? throw new AbilityException(
                $"card '{card.FaceId}' has no '{ability.Type}' ability to describe");

        // The ability's own name is the verb, which is the engine's convention:
        // `datasets/digest/prompts.json` offers `Foresight` and `"I_Object!"`,
        // both card names. One string does for both fields because the engine
        // carries one -- see the remarks on `Affordance.Id`.
        var price = Price(world, card, ability.Player, found.Cost);
        return new Affordance(
            Id: ability.Card,
            Verb: found.Name,
            AnchorId: ability.Card,
            AnchorPlayer: ability.Player,
            Label: found.Name,
            Costs: price is null ? null : [price]);
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Resolve(
        World world, Occurrence occurrence, PendingAbility ability)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);

        var card = world.Cards[ability.Card];
        var found = book.On(card.FaceId)
            .Where(candidate => candidate.Trigger.Timing == ability.Type)
            .ToList();

        if (found.Count != 1)
        {
            // Two abilities of one type on one card cannot be told apart by a
            // `PendingAbility`, which names a card and a tier. A card that needs
            // it needs the pending ability to carry which one, and that is a
            // change to make when a card demands it rather than now.
            throw new AbilityException(
                $"card '{card.FaceId}' has {found.Count} '{ability.Type}' abilities, "
                + "and exactly one can be resolved from a window");
        }

        var events = new List<GameEvent>();

        // **Who "you" is, which is not who may trigger it.**
        // `PendingAbility.Player` is control -- `rr:ability.8` lets any player
        // use an optional ability on an encounter card, so an encounter card's
        // is the scenario. That is the right answer to "whose opportunity is
        // this" and the wrong one to "who does the card mean by *you*".
        //
        // `rr:you-your.7` is explicit for the case this arrived on: "for
        // abilities that trigger 'after [enemy] attacks you,' 'you' refers to
        // the attacked player, even if that player defended with an ally." The
        // attacked player is the occurrence's, so an ability on a card nobody
        // owns resolves as the player the occurrence happened to. `.16` is not
        // in the way -- it says an encounter card's ability is not performed by
        // that player's identity, which is about who acts, not about who the
        // word points at.
        int resolving = ability.Player >= 0 ? ability.Player : occurrence.Player;
        var cast = new Cast(world, card, occurrence, resolving, events, this);

        // `rr:initiating-abilities` keeps the steps apart, and step 5 pays
        // before step 6 resolves. Nothing here can abort, because step 3 --
        // `Payable`, when the ability was offered -- already asked whether it
        // could be paid.
        Pay(found[0].Cost, [], cast);
        Run(found[0].Effect, cast);
        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        if (!book.Authored.Contains(card.FaceId))
        {
            // Authored-and-does-nothing is a different thing from nobody having
            // read the card, and only one of them is safe to treat as silence.
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' was revealed and no ability data is written for it; "
                + $"this engine has {book.Authored.Count} authored card(s)");
        }

        var events = new List<GameEvent>();

        // `rr:reveal` is the occurrence; the card is not in play while it
        // resolves, which is why this does not go through `Waiting`.
        var occurrence = new Occurrence(
            0, [Steps.CardRevealed], Subject: card.ObjectId, Player: player);

        foreach (var ability in book.On(card.FaceId))
        {
            // `rr:ability.step.3` -- "When Revealed" *is* the occurrence, not a
            // window around it. An interrupt or a response to a card being
            // revealed is a different ability and reaches the board through
            // `Waiting`, so matching on the condition alone would run it twice.
            if (ability.Trigger.Timing == AbilityType.WhenRevealed
                && string.Equals(ability.Trigger.Event, Steps.CardRevealed, StringComparison.Ordinal))
            {
                Run(ability.Effect, new Cast(world, card, occurrence, player, events, this));
            }
        }

        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Boost(World world, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        // **Not "is the card authored" but "is this half of it".** A card with
        // two abilities at two tiers -- `01168` Sweeping Swoop has a "When
        // Revealed" and a "Boost" -- would otherwise pass on the strength of
        // the half somebody had written, and the other half would go back to
        // being silent.
        var boosts = book.On(card.FaceId)
            .Where(ability => ability.Trigger.Timing == AbilityType.Boost)
            .ToList();

        if (boosts.Count == 0)
        {
            // **The star gates the complaint, not the run.** The printed
            // `Boost` attribute counts icons and `rr:boost-boost-icon.1` says a
            // star is not one, so a card with an ability and a card without
            // carry the same number and only the text box can tell them apart.
            // Asked here rather than first, so that the text box cannot veto
            // authored data.
            return world.Facts.HasBoostAbility(card.FaceId)
                ? throw new RulesNotImplementedException(
                    $"card '{card.FaceId}' was turned faceup as a boost card and prints a "
                    + "'Boost' ability that no ability data is written for")
                : [];
        }

        var events = new List<GameEvent>();
        var occurrence = new Occurrence(
            0, [Steps.CardRevealed], Subject: card.ObjectId, Player: player);

        foreach (var ability in boosts)
        {
            // `rr:ability` puts a "Boost" ability at the occurrence tier, like
            // "When Revealed": it is the thing happening rather than a window
            // around it, so there is nothing to offer and nothing to decline.
            Run(ability.Effect, new Cast(world, card, occurrence, player, events, this));
        }

        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ResourceSource> ResourceAbilities(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);

        var sources = new List<ResourceSource>();
        foreach (var card in Triggerable(world, player))
        {
            foreach (var ability in book.On(card.FaceId))
            {
                if (ability.Trigger.Timing != AbilityType.Resource
                    || !Available(world, card, ability))
                {
                    continue;
                }

                // The letters this makes, read off the effect rather than the
                // printed `RES` field: `RES` is what discarding the card
                // generates, and an ability is a different way to make one.
                sources.Add(new ResourceSource(card.ObjectId, Generated(ability.Effect)));
            }
        }

        return sources;
    }

    /// <summary>
    /// Whether an ability has uses left this round — <c>rr:limit</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Each copy of an ability with such a limit may be used X times per the
    /// specified period, <b>per instance of that ability</b>", so the count is
    /// kept against the card in play rather than the printed id: two Peter
    /// Parkers at one table have one use each.
    /// </para>
    /// <para>
    /// <b>Kept as a lasting effect and not a token.</b> A card's tokens are on
    /// the wire — they are the digest's <c>fields</c> — so counting uses there
    /// would put a number in every recorded board that the recording does not
    /// have. A lasting effect is not digested, and it expires at the end of the
    /// round without anything having to remember to clear it.
    /// </para>
    /// </remarks>
    private static bool Available(World world, Card card, CardAbility ability) =>
        ability.Limit is not { } limit
        || world.Effects.Active().Count(effect =>
            effect.Card == card.ObjectId
            && string.Equals(effect.Kind, Spent(ability), StringComparison.Ordinal)) < limit;

    /// <summary>Records one use of a limited ability, until the round ends.</summary>
    private static void Use(World world, Card card, CardAbility ability)
    {
        if (ability.Limit is not null)
        {
            world.Effects.Register(new ContinuousEffect(
                EffectSource.LastingEffect,
                Kind: Spent(ability),
                Card: card.ObjectId,
                Affects: card.ObjectId,
                Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));
        }
    }

    /// <summary>The effect kind that stands for one use of an ability.</summary>
    private static string Spent(CardAbility ability) => "spent:" + ability.Name;

    /// <summary>What letters an effect generates, if it only generates.</summary>
    private static string Generated(AbilityNode effect) => effect.Kind == "generate"
        ? Word(effect.Argument)
        : throw new RulesNotImplementedException(
            $"a resource ability whose effect is '{effect.Kind}' generates nothing this "
            + "engine can read");

    /// <inheritdoc/>
    public string UseResource(World world, int player, int card)
    {
        ArgumentNullException.ThrowIfNull(world);

        var holder = world.Cards[card];
        var ability = book.On(holder.FaceId).FirstOrDefault(candidate =>
            candidate.Trigger.Timing == AbilityType.Resource
            && Available(world, holder, candidate))
            ?? throw new RulesNotImplementedException(
                $"card {card} has no resource ability left to use this round");

        Use(world, holder, ability);
        return Generated(ability.Effect);
    }

    /// <inheritdoc/>
    public long WouldBeDealt(World world, Card target, long amount, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(events);

        if (amount <= 0)
        {
            return amount;
        }

        var occurrence = new Occurrence(
            0, [Steps.DamageWouldBeDealt], Subject: target.ObjectId, Player: target.Owner);

        long left = amount;
        foreach (var (card, ability) in Waiting(world, occurrence))
        {
            // **Forced only.** `rr:ability.11` makes everything optional unless
            // prefaced by "Forced", and an optional interrupt is a question --
            // which needs a window, which dealing damage has not got. A card
            // that would ask here is refused by name rather than resolved
            // without asking.
            if (ability.Trigger.Timing != AbilityType.ForcedInterrupt)
            {
                throw new RulesNotImplementedException(
                    $"'{card.FaceId}' asks to interrupt damage, and dealing damage opens "
                    + "no window for an optional ability");
            }

            var cast = new Cast(world, card, occurrence, target.Owner, events, this)
            {
                Incoming = left,
            };

            Run(ability.Effect, cast);

            // An ability that touched the damage says so; one that did nothing
            // to it leaves it alone. `rr:damage.step.1` holds abilities that
            // *may* replace the damage, not ones that must.
            left = cast.Remaining < 0 ? left : cast.Remaining;
            if (left <= 0)
            {
                // `rr:replacement-effect.1` -- "when an effect is replaced, it
                // is no longer considered imminent and no further interrupts or
                // responses to that effect can be triggered."
                return 0;
            }
        }

        return left;
    }

    /// <summary>Every authored ability answering one occurrence, with its card.</summary>
    /// <remarks>
    /// <b>Gathered before any of it runs.</b> An ability can make an area —
    /// giving a status card creates one to hold it — and walking
    /// <c>World.Areas</c> lazily while resolving would be modifying the
    /// collection being read.
    /// </remarks>
    private List<(Card Card, CardAbility Ability)> Waiting(World world, Occurrence what) =>
    [
        .. world.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .ToList()
            .SelectMany(card => book.On(card.FaceId)
                .Where(ability => Answers(ability, card, what))
                .Select(ability => (Card: card, Ability: ability)))
            .ToList(),
    ];

    /// <summary>Whether one ability answers this occurrence at all.</summary>
    private static bool Answers(CardAbility ability, Card card, Occurrence what) =>
        what.Conditions.Contains(ability.Trigger.Event, StringComparer.Ordinal)
        && Subject(ability.Trigger.Subject, card, what);

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> WhenDefeated(World world, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);

        var defeated = book.On(card.FaceId)
            .Where(ability => ability.Trigger.Timing == AbilityType.WhenDefeated)
            .ToList();

        if (defeated.Count == 0)
        {
            // **The printed check gates the complaint, not the run.** Nothing
            // in the printed attributes records a "When Defeated", so an
            // unwritten one and a card that has none look identical from here
            // -- but that is only a question when there is nothing written.
            // Asking it first would let the text box veto authored data, which
            // is the wrong way round: the data is what the engine runs.
            return world.Facts.HasWhenDefeated(card.FaceId)
                ? throw new RulesNotImplementedException(
                    $"card '{card.FaceId}' was defeated and prints a 'When Defeated' "
                    + "ability that no ability data is written for")
                : [];
        }

        var events = new List<GameEvent>();

        // `rr:when-defeated-abilities.2` -- "**all** When Defeated abilities on
        // the card resolve", so this is every one of them rather than the
        // single one a window would take.
        var occurrence = new Occurrence(
            0, [Steps.CardDefeated], Subject: card.ObjectId, Player: card.Owner);

        foreach (var ability in defeated)
        {
            Run(ability.Effect, new Cast(world, card, occurrence, card.Owner, events, this));
        }

        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Act(
        World world, PendingAbility ability, IReadOnlyList<int> paying)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(paying);

        var card = world.Cards[ability.Card];
        var found = book.On(card.FaceId)
            .SingleOrDefault(candidate => candidate.Trigger.Timing == ability.Type)
            ?? throw new AbilityException(
                $"card '{card.FaceId}' has no single '{ability.Type}' ability to trigger");

        var events = new List<GameEvent>();
        var cast = new Cast(
            world,
            card,
            new Occurrence(0, [Steps.TurnAction], Subject: card.ObjectId, Player: ability.Player),
            ability.Player,
            events,
            this);

        // `rr:initiating-abilities` keeps the steps apart, and step 5 pays
        // before step 6 resolves.
        Pay(found.Cost, paying, cast);
        Run(found.Effect, cast);
        return events;
    }

    /// <inheritdoc/>
    public IReadOnlyList<PendingAbility> Actions(World world, int player)
    {
        ArgumentNullException.ThrowIfNull(world);

        var found = new List<PendingAbility>();
        foreach (var card in Triggerable(world, player))
        {
            foreach (var ability in book.On(card.FaceId))
            {
                if (ability.Trigger.Timing == AbilityType.Action
                    && InForm(world, player, ability.Trigger.Form)
                    && Payable(world, card, player, ability.Cost))
                {
                    found.Add(new PendingAbility(card.ObjectId, AbilityType.Action, player));
                }
            }
        }

        return found;
    }

    /// <summary>
    /// The cards one player may trigger an action on —
    /// <c>rr:player-turn.5</c>'s four places.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>.a</c> "a card in play they control", <c>.b</c> "an encounter card in
    /// play", <c>.d</c> "an event card in their hand <i>(by playing that
    /// event)</i>". <c>.c</c> — "any card in play with text that allows that
    /// player to trigger its action ability" — is a card's own text and belongs
    /// to whichever card says it, so there is nothing general to write here.
    /// </para>
    /// <para>
    /// <b>An event is reached from the hand and nowhere else.</b> That is why
    /// <c>CardPlay.Price</c> refuses to offer one: an event is not
    /// <c>rr:player-turn.2</c>'s "ally, upgrade, support, or player side
    /// scheme", it is played by triggering its action.
    /// </para>
    /// </remarks>
    private static IEnumerable<Card> Triggerable(World world, int player)
    {
        foreach (var area in world.Areas)
        {
            if (!DeckTypes.IsInPlay(area.Type))
            {
                continue;
            }

            foreach (var card in area.Cards)
            {
                // `.a` and `.b`: yours, or nobody's. A card another player
                // controls is theirs to trigger -- `rr:player-turn.6` is how
                // you ask them.
                if (card.Owner == player || card.Owner == World.Scenario)
                {
                    yield return card;
                }
            }
        }

        // `.d`, and only events: an ally in hand is played rather than
        // triggered, and `rr:player-turn.2` is where that happens.
        foreach (var card in world.Seats[player].Hand.Cards)
        {
            if (world.Facts.Kind(card.FaceId) == CardKind.Event)
            {
                yield return card;
            }
        }
    }

    /// <summary>
    /// Whether the player is in the form an ability requires —
    /// <c>rr:player-turn.5.1</c>.
    /// </summary>
    private static bool InForm(World world, int player, string? form) =>
        form is null || Forms.In(world, world.Seats[player], world.Facts, form);

    /// <summary>
    /// Whether an ability's cost can be paid — <c>rr:initiating-abilities.step.3</c>.
    /// </summary>
    /// <remarks>
    /// Asked before the ability is offered, because "the player's ability to pay
    /// them" is step 3 and step 5 aborts "without paying any costs" — so an
    /// ability that would abort is not an offer, it is a trap. An exhausted card
    /// cannot pay a cost of exhausting itself: <c>rr:exhausted.2</c>.
    /// </remarks>
    private static bool Payable(World world, Card card, int player, AbilityNode? cost) =>
        cost switch
        {
            null => true,
            { Kind: "exhaust" } => card.Ready,

            // Asked of the whole hand, which is the right question rather than
            // an approximation: `rr:cost.4` permits generating beyond the cost,
            // so if everything together cannot pay then no choice among them
            // can, and if it can then spending it all is a payment.
            { Kind: "spend" } => Resources.Pays(
                string.Concat(CardPlay.Generators(world, world.Facts, world.Seats[player])
                    .SelectMany(source => source.Generates)),
                Word(cost.Argument).Length,
                Word(cost.Argument)),

            _ => throw new RulesNotImplementedException(
                $"'{card.FaceId}' has a cost of '{cost.Kind}', which is not implemented"),
        };

    /// <summary>What an action's cost looks like on a prompt, or null.</summary>
    /// <remarks>
    /// Only a resource cost reaches the wire, because only a resource cost is a
    /// <i>choice</i>. Exhausting the card the ability is on has one way to be
    /// paid, so there is nothing to ask and nothing to carry.
    /// </remarks>
    private static CostOption? Price(World world, Card card, int player, AbilityNode? cost)
    {
        if (cost is not { Kind: "spend" })
        {
            return null;
        }

        string letters = Word(cost.Argument);
        return new CostOption(
            Target: card.ObjectId,
            Cost: letters.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Rule: [letters],
            Sources: CardPlay.Generators(world, world.Facts, world.Seats[player]));
    }

    /// <summary>Pays an ability's cost — <c>rr:initiating-abilities.step.5</c>.</summary>
    private static void Pay(AbilityNode? cost, IReadOnlyList<int> paying, Cast cast)
    {
        if (cost is null)
        {
            return;
        }

        if (cost.Kind == "spend")
        {
            string letters = Word(cost.Argument);
            CardPlay.Spend(
                cast.World,
                cast.World.Facts,
                [cast.World.Seats[cast.Player].Hand],
                paying,
                letters.Length,
                letters,
                itself: -1,
                cast.Player,
                cast.Events);
            return;
        }

        Run(cost, cast);
    }

    /// <inheritdoc/>
    public Prompt? Choosing(World world, Card source, int player, int stoppedAt)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);

        var choice = Choice(source, stoppedAt);

        if (choice.Kind == "indirectDamage")
        {
            return Sharing(world, source, player, choice);
        }

        bool cards = choice.Kind == "chooseCard";

        // `rr:choose-option` and `rr:choose-game-element` are two questions and
        // not one: an option is a branch the card lists, an element is a card
        // on the board. `Question` has told them apart since before anything
        // asked either.
        var affordances = cards
            ? Every(choice.Require("from"), Resolving(world, source, player))
                .Select(card => new Affordance(
                    Id: card.ObjectId,
                    Verb: ChooseVerb,
                    AnchorId: card.ObjectId,
                    AnchorPlayer: card.Owner,
                    Label: card.FaceId))
            : Nodes(choice.Require("options")).Select((option, index) => new Affordance(
                Id: index,
                Verb: ChooseVerb,
                AnchorId: source.ObjectId,
                AnchorPlayer: World.Scenario,
                Label: option.Kind));

        return new Prompt(
            Player: player,
            Asking: cards ? Question.Element : Question.Option,
            When: TimingPriority.Untimed,
            Trigger: Steps.CardRevealed,
            Label: $"{source.FaceId}: choose {(cards ? "a card" : "an option")}",

            // Neither rule gives a way out. The ability is resolving, and one
            // of the things it offers is going to happen.
            Cancellable: false,
            Affordances: [.. affordances]);
    }

    /// <summary>
    /// The question an assignment asks — <c>rr:indirect-damage.1</c>.
    /// </summary>
    /// <remarks>
    /// One answer naming a character per point, so the same character may
    /// appear more than once: assigning three damage to one hero is three
    /// entries. <c>rr:choose-game-element.3.1</c>'s "the same target cannot be
    /// chosen multiple times" is about <i>targets</i>, and this is a division
    /// rather than a target list.
    /// </remarks>
    private Prompt Sharing(World world, Card source, int player, AbilityNode choice)
    {
        var cast = Resolving(world, source, player);
        long amount = Amount(choice.Require("amount"), cast);
        var eligible = Assignable(choice.Require("among"), cast);

        // `rr:indirect-damage.3.1` -- never more than would defeat a character,
        // so an assignment can be short of the amount when the table has less
        // room than the card has damage.
        long share = Math.Min(amount, eligible.Sum(card => Room(cast, card)));

        return new Prompt(
            Player: player,
            Asking: Question.Element,
            When: TimingPriority.Untimed,
            Trigger: Steps.CardRevealed,
            Label: $"{source.FaceId}: assign {share} damage",
            Cancellable: false,
            Affordances:
            [
                new Affordance(
                    Id: source.ObjectId,
                    Verb: ChooseVerb,
                    AnchorId: source.ObjectId,
                    AnchorPlayer: World.Scenario,
                    Label: choice.Kind,
                    Targets: new TargetRequest(
                        Legal: [.. eligible.Select(card => card.ObjectId)],
                        Min: (int)share,
                        Max: (int)share,
                        Rule: "rr:indirect-damage.1")),
            ]);
    }

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Chose(
        World world, Card source, int player, int stoppedAt, Decision input)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(input);

        var choice = Choice(source, stoppedAt);
        var cast = Resolving(world, source, player);

        if (choice.Kind == "indirectDamage")
        {
            var eligible = Assignable(choice.Require("among"), cast);
            var chosen = new List<Card>();
            foreach (int id in input.Targets)
            {
                chosen.Add(
                    eligible.FirstOrDefault(card => card.ObjectId == id)
                    ?? throw new RulesNotImplementedException(
                        $"card {id} cannot be assigned indirect damage from "
                        + $"'{source.FaceId}'"));
            }

            // One point per entry, so a character named three times takes
            // three. `rr:indirect-damage.3` resolves the whole assignment at
            // once, which is why the counts are gathered before any of it is
            // dealt.
            var share = new Dictionary<int, long>();
            foreach (var card in chosen)
            {
                share[card.ObjectId] = share.GetValueOrDefault(card.ObjectId) + 1;
            }

            Resolve(cast, share);
            return Continue(source, cast, stoppedAt);
        }


        if (choice.Kind == "chooseCard")
        {
            cast.Choose(
                Every(choice.Require("from"), cast)
                    .FirstOrDefault(card => card.ObjectId == input.Affordance)
                ?? throw new RulesNotImplementedException(
                    $"'{source.FaceId}' did not offer card {input.Affordance} to choose"));

            Run(Tree(choice.Require("effect")), cast);
            return Continue(source, cast, stoppedAt);
        }

        var options = Nodes(choice.Require("options")).ToList();
        if (input.IsDecline || input.Affordance < 0 || input.Affordance >= options.Count)
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' offers {options.Count} options and none of them is "
                + $"number {input.Affordance}");
        }

        Run(options[input.Affordance], cast);
        return Continue(source, cast, stoppedAt);
    }

    /// <summary>
    /// Runs what is left of the ability after the answered choice.
    /// </summary>
    /// <remarks>
    /// The chosen option has already run; this is the rest of the sequence it
    /// was a step of. If the rest holds another choice, it suspends again and
    /// the step it schedules says where to pick up next.
    /// </remarks>
    private List<GameEvent> Continue(Card source, Cast cast, int from)
    {
        var effect = book.On(source.FaceId)
            .Select(ability => ability.Effect)
            .FirstOrDefault(tree => Choices(tree).Any());

        // **The resume point belongs to the top-level sequence and nowhere
        // else.** Carried on the `Cast` it would leak into any `seq` the chosen
        // option itself contains -- an option of three effects resumed at two
        // would run only the third.
        if (effect is { Kind: "seq" } && !cast.Suspended)
        {
            Sequence(effect, cast, from);
        }

        return cast.Events;
    }

    /// <summary>A fresh resolution of one card's ability, by one player.</summary>
    private Cast Resolving(World world, Card source, int player) =>
        new(world,
            source,
            new Occurrence(0, [Steps.CardRevealed], Subject: source.ObjectId, Player: player),
            player,
            [],
            this);

    /// <summary>The one choice a card offers, found again from the card.</summary>
    /// <remarks>
    /// A step cannot carry an effect tree, so it carries the card and the node
    /// is looked up again. That makes "exactly one choice per card" the price,
    /// and it is charged by name: a second one would make which of them is
    /// waiting a guess.
    /// </remarks>
    private AbilityNode Choice(Card source, int stoppedAt)
    {
        // **Which choice, when a card has several.** The step says where the
        // ability stopped, and the choice that stopped it is the step before
        // that. A card whose whole effect is one choice has no sequence at all,
        // and resumes at one.
        var effect = book.On(source.FaceId)
            .Select(ability => ability.Effect)
            .FirstOrDefault(tree => Choices(tree).Any())
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no choice waiting on an answer");

        if (effect.Kind != "seq")
        {
            return Choices(effect).Single();
        }

        var steps = Nodes(effect.Argument).ToList();
        return stoppedAt >= 1 && stoppedAt <= steps.Count
            && steps[stoppedAt - 1] is
                { Kind: "choose" or "chooseCard" or "indirectDamage" } waiting
            ? waiting
            : throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no choice at step {stoppedAt - 1} of its sequence");
    }

    /// <summary>Every <c>choose</c> node in one effect tree.</summary>
    private static IEnumerable<AbilityNode> Choices(AbilityNode node)
    {
        if (node.Kind is "choose" or "chooseCard" or "indirectDamage")
        {
            yield return node;
            yield break;
        }

        var children = node.Kind switch
        {
            "seq" => Nodes(node.Argument),
            "if" => Branches
                .Select(node.Field)
                .Where(branch => branch is not null)
                .Select(branch => Tree(branch!)),
            _ => [],
        };

        foreach (var found in children.SelectMany(Choices))
        {
            yield return found;
        }
    }

    /// <summary>Whether one ability answers this occurrence, in this window.</summary>
    private static bool Answers(
        CardAbility ability, Card card, Occurrence occurrence, WindowKind window)
    {
        if (!occurrence.Is(ability.Trigger.Event))
        {
            return false;
        }

        bool belongs = window switch
        {
            WindowKind.Interrupt => AbilityTypes.IsInterrupt(ability.Trigger.Timing),
            WindowKind.Response => AbilityTypes.IsResponse(ability.Trigger.Timing),
            _ => false,
        };

        return belongs && Subject(ability.Trigger.Subject, card, occurrence);
    }

    private static bool Subject(string subject, Card card, Occurrence occurrence) => subject switch
    {
        AbilitySubjects.This => occurrence.Subject == card.ObjectId,
        AbilitySubjects.AttachedTo => card.Area.Host >= 0 && occurrence.Subject == card.Area.Host,
        AbilitySubjects.You => occurrence.Player >= 0 && occurrence.Player == card.Owner,
        _ => throw new AbilityException($"'{subject}' is not a subject anything matches"),
    };

    // ---- the effect tree ---------------------------------------------------

    private static void Run(AbilityNode node, Cast cast)
    {
        switch (node.Kind)
        {
            case "seq":
                Sequence(node, cast, from: 0);
                break;

            case "generate":
                // `rr:resource-ability` -- a resource ability is *read* while a
                // cost is being paid rather than run like an effect, so nothing
                // happens here. `ResourceAbilities` takes its letters and
                // `UseResource` counts the use; running it would be a second
                // way to generate the same resource.
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' generates a resource, which is read while a "
                    + "cost is paid rather than resolved as an effect");

            case "changeForm":
                ChangeForm(node, cast);
                break;

            case "removeFromGame":
                RemoveFromGame(node, cast);
                break;

            case "soakDamage":
                Soak(node, cast);
                break;

            case "exhaust":
                Exhaust(node, cast);
                break;

            case "revealTop":
                RevealCard(TopOfTheEncounterDeck(cast), cast);
                break;

            case "reveal":
                RevealCard(Find(node.Argument, cast), cast);
                break;

            case "placeAtRandom":
                PlaceAtRandom(node, cast);
                break;

            case "returnToHand":
                ReturnToHand(node, cast);
                break;

            case "discardAtRandom":
                DiscardAtRandom(node, cast);
                break;

            case "discardUntil":
                DiscardUntil(node, cast);
                break;

            case "shuffleInto":
                ShuffleInto(node, cast);
                break;

            case "search":
                Search(node, cast);
                break;

            case "choose":
            case "chooseCard":
                Choose(node, cast);
                break;

            case "if":
                var branch = Test(Tree(node.Require("test")), cast) ? "then" : "else";
                if (node.Field(branch) is { } taken)
                {
                    Run(Tree(taken), cast);
                }

                break;

            case "giveStatus":
                GiveStatus(node, cast);
                break;

            case "attachTo":
                AttachTo(node, cast);
                break;

            case "grantUntil":
                GrantUntil(node, cast);
                break;

            case "delayUntil":
                DelayUntil(node, cast);
                break;

            case "discard":
                Discard(node, cast);
                break;

            case "gainSurge":
                // `rr:surge`: "the player resolving the card deals themself a
                // facedown encounter card from the top of the encounter deck",
                // and `.1` writes it as "**When Revealed**: deal yourself 1
                // facedown encounter card". A card that *gains* surge does the
                // same thing the keyword would have -- so this is one deal, and
                // the number beside the node is how many.
                //
                // `.2` finishes the original card first, which the villain
                // phase's reveal queue does without anything here.
                for (long dealt = 0; dealt < Number(node.Argument); dealt++)
                {
                    Deal.EncounterCard(
                        cast.World, cast.Player, cast.Occurrence.Conditions[0], cast.Events);
                }

                break;

            case "heal":
                Heal(node, cast);
                break;

            case "indirectDamage":
                Indirect(node, cast);
                break;

            case "dealDamage":
                DealDamage(node, cast);
                break;

            case "placeThreat":
                PlaceThreat(node, cast);
                break;

            case "enemyAttacks":
                Activate(node, cast, Steps.Attack);
                break;

            case "enemySchemes":
                Activate(node, cast, Steps.Scheme);
                break;

            case "draw":
                Draw.Cards(
                    cast.World, Seat(node.Require("player"), cast),
                    (int)Number(node.Require("count")),
                    cast.Occurrence.Conditions[0], cast.Events);
                break;

            default:
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' uses the effect node '{node.Kind}', "
                    + "which is not implemented");
        }
    }

    private static bool Test(AbilityNode node, Cast cast) => node.Kind switch
    {
        "and" => Nodes(node.Argument).All(each => Test(each, cast)),
        "or" => Nodes(node.Argument).Any(each => Test(each, cast)),
        "not" => !Test(Tree(node.Argument), cast),
        // Through `Every` and not `Find`: "is there one" is a question about a
        // set, and a query that names many -- "an upgrade or support you
        // control" -- has to be answerable by it. `Every` falls back to `Find`
        // for the queries that name one, so both shapes go through here.
        "exists" => Every(node.Argument, cast).Count > 0,

        // "If Vulture is in play". `rr:identity.2` makes a title name one
        // card -- "if a card refers to a hero or alter-ego by title, it refers
        // only to the identity with that title" -- so this compares titles and
        // not printed ids, and asks only of the places `rr:in-play-and-out-of-play`
        // calls in play.
        "titleInPlay" => cast.World.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Any(card => string.Equals(
                cast.World.Facts.Title(card.FaceId), Word(node.Argument),
                StringComparison.Ordinal)),

        // "If no damage was healed this way" and its family: a comparison
        // against what an earlier action in this ability actually did.
        "atLeast" => Amount(node.Require("value"), cast) >= Amount(node.Require("count"), cast),

        // `rr:form` -- "(Hero)" and "(Alter-Ego)" on a card gate the ability by
        // which form the player is in. Not a boolean: `Forms.Of` answers with a
        // set, because a hero can print more than two faces.
        "inForm" => Forms.In(
            cast.World,
            cast.World.Seats[Seat(node.Require("player"), cast)],
            cast.World.Facts,
            Word(node.Require("form"))),

        // "After [enemy] attacks **and damages** you". Two facts, and
        // `rr:attack-enemy-activation.step.6.a` lists them as one trigger
        // shape -- but the abilities it lists all run in the window *after* the
        // attack, by which time the damage is on a dial that had damage on it
        // before. So the attack carries what it did, and this reads it.
        //
        // A test rather than a triggering condition of its own, because the two
        // are indistinguishable for a forced ability: it is in the same window
        // either way, and does nothing when the attack did not land. A card
        // whose trigger is optional would be able to tell them apart -- the
        // prompt would appear -- and that is the case to change this for.
        "attackDamaged" => cast.World.FinishedAttack is { Damaged: true } landed
            && landed.Enemy == cast.Source.ObjectId,

        "hasStatus" => Find(node.Require("card"), cast) is { } host
            && Statuses.Has(cast.World, host, Word(node.Require("status"))),
        _ => throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' uses the test node '{node.Kind}', "
            + "which is not implemented"),
    };

    private static void GiveStatus(AbilityNode node, Cast cast)
    {
        // "Stun **each hero**" and "stun your hero" are the same node with a
        // different query, the way `placeThreat` names one scheme or all of
        // them: `Every` answers both.
        var hosts = Every(node.Require("card"), cast);
        if (hosts.Count == 0)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would give a status to a card that is not there");
        }

        foreach (var host in hosts)
        {
            GiveStatus(node, cast, host);
        }
    }

    private static void GiveStatus(AbilityNode node, Cast cast, Card host)
    {
        string what = Word(node.Require("status"));

        // Through the rules rather than straight at `Statuses.Give`:
        // `rr:status-cards.1` caps how many a character can hold,
        // `rr:stalwart` makes that cap zero, and `rr:vulnerable` discards the
        // character. A card giving a status does not get to skip any of them.
        var status = Reveal.Afflict(
            cast.World, cast.World.Facts, host, what, cast.Trigger, cast.Events);
        if (status is null)
        {
            return;
        }

        cast.Events.Add(new CardAttached(status.ObjectId, host.ObjectId)
        {
            Trigger = cast.Trigger, Verb = "Give_Status",
        });
    }

    // `rr:attachment` -- "when an attachment enters play, it attaches to another
    // card or game element".
    private static void AttachTo(AbilityNode node, Cast cast)
    {
        var host = Find(node.Argument, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' attaches to a card that is not there");

        var onto = cast.World.AreaOf(
            DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId, host.Area.CardOwner);
        var from = cast.Source.Area;
        World.MoveToTop(cast.Source, onto);

        cast.Events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(onto),
            [new Landing(cast.Source.ObjectId, onto.Cards.Count - 1)])
        {
            Trigger = cast.Trigger, Verb = "Attach",
        });
        cast.Events.Add(new CardAttached(cast.Source.ObjectId, host.ObjectId)
        {
            Trigger = cast.Trigger, Verb = "Attach",
        });
    }

    // `rr:lasting-effects` -- an effect "for a specified duration (such as
    // [...] 'until the end of this attack')".
    private static void GrantUntil(AbilityNode node, Cast cast)
    {
        var target = Find(node.Require("card"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would grant to a card that is not there");

        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: Word(node.Require("keyword")),
            Amount: node.Field("amount") is { } amount ? Number(amount) : 0,
            Card: cast.Source.ObjectId,
            Affects: target.ObjectId,
            Lasts: Duration.UntilEndOf(Word(node.Require("until")))));
    }

    // `rr:delayed-effect.1` -- an effect that resolves "after their specified
    // timing point or future condition occurs or becomes true".
    private static void DelayUntil(AbilityNode node, Cast cast)
    {
        var effect = Tree(node.Require("effect"));

        // "If a character is damaged by this attack, that character is
        // stunned." **The card it acts on does not exist yet** -- the attack
        // has not happened, so there is nobody to name. `Affects` stays null
        // and the occurrence names the card when the effect comes due.
        if (effect.Kind == "giveStatus"
            && Word(effect.Require("card")) == "damaged"
            && Word(effect.Require("status")) == Statuses.Stunned)
        {
            // **Bounded by the attack as well as by the condition.** "If a
            // character is damaged by **this attack**" is false once the attack
            // is over, so an attack that damaged nobody -- `rr:tough.3`, a
            // tough status card ate it -- must not leave the effect waiting for
            // somebody else's. `Duration` carries both: the next time damage is
            // dealt, and not past the end of this attack.
            cast.World.Effects.Register(new ContinuousEffect(
                EffectSource.DelayedEffect,
                Kind: DelayedEffects.StunTheSubject,
                Card: cast.Source.ObjectId,
                Affects: null,
                Lasts: new Duration(
                    Until: node.Field("within") is { } bound ? Word(bound) : null,
                    OnCondition: Word(node.Require("condition")),
                    Uses: 1)));
            return;
        }

        if (effect.Kind != "discard")
        {
            // A delayed effect is data on the board, not a closure, so what it
            // will do has to be a `Kind` the engine can read back after a save.
            // `DelayedEffects` knows one; the rest is the vocabulary that grows.
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' delays '{effect.Kind}', and only 'discard' can be "
                + "written down as a delayed effect");
        }

        var target = Find(effect.Field("card") ?? effect.Argument, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would delay a discard of a card that is not there");

        cast.World.Effects.Register(new ContinuousEffect(
            EffectSource.DelayedEffect,
            Kind: DelayedEffects.DiscardFromPlay,
            Card: cast.Source.ObjectId,
            Affects: target.ObjectId,
            Lasts: Duration.NextTime(Word(node.Require("condition")))));
    }

    private static void Discard(AbilityNode node, Cast cast)
    {
        var target = Find(node.Field("card") ?? node.Argument, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would discard a card that is not there");

        Rules.Play.Discard.Card(cast.World, target, cast.Trigger, cast.Events);
    }

    // ---- reading a value ---------------------------------------------------

    /// <summary>
    /// "Flip to alter-ego form" — <c>rr:form-change-form</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It does not use up the turn's flip.</b> <c>rr:form-change-form.3</c>:
    /// "if a card ability causes a player to change forms, it does not count
    /// against the one voluntary form change the player is permitted during
    /// their turn that round." So this goes through <c>Forms.Change</c>, which
    /// turns the card, and leaves <c>Seat.FormChangedInRound</c> alone —
    /// <c>Game</c> sets that when the player takes the turn option.
    /// </para>
    /// <para>
    /// A player already in the named form does nothing. "Flip <b>to</b>
    /// alter-ego form" names a destination, and flipping an alter-ego would
    /// arrive at the wrong one.
    /// </para>
    /// </remarks>
    private static void ChangeForm(AbilityNode node, Cast cast)
    {
        var seat = cast.World.Seats[Seat(node.Require("player"), cast)];
        string form = Word(node.Require("to"));
        if (Forms.In(cast.World, seat, cast.World.Facts, form))
        {
            return;
        }

        string was = seat.IdentityCard.FaceId;
        Forms.Change(seat, cast.World.Facts);
        cast.Events.Add(new CardsFlipped([seat.IdentityCard.ObjectId], true)
        {
            Trigger = cast.Trigger, Verb = "Change_Form",
        });

        if (!Forms.In(cast.World, seat, cast.World.Facts, form))
        {
            throw new RulesNotImplementedException(
                $"flipping '{was}' did not reach {form}");
        }
    }

    /// <summary>"Remove … from the game" — <c>rr:removed-from-the-game</c>.</summary>
    /// <remarks>
    /// Removed and not discarded: <c>rr:defeat.2</c> keeps the two apart, and a
    /// card in the discard pile can come back where one out of the game cannot.
    /// </remarks>
    private static void RemoveFromGame(AbilityNode node, Cast cast)
    {
        var card = Find(node.Argument, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would remove a card that is not there");

        var from = card.Area;
        var removed = cast.World.AreaOf(DeckType.RemovedArea);
        World.MoveToTop(card, removed);
        cast.Events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(removed),
            [new Landing(card.ObjectId, removed.Cards.Count - 1)])
        {
            Trigger = cast.Trigger, Verb = "Remove_From_Game",
        });
    }

    /// <summary>
    /// "Place it here instead" — <c>rr:replacement-effect</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The damage does not happen to the character at all: it is <i>placed</i>
    /// on this card as damage tokens, which is why it goes on with
    /// <c>Card.TakeDamage</c> rather than through <c>Damage.Deal</c>. Dealing it
    /// would start the nine steps of <c>rr:damage</c> again, on a card that is
    /// not a character.
    /// </para>
    /// <para>
    /// What is left afterwards is zero, and <c>rr:replacement-effect.1</c> then
    /// holds for free: the damage is no longer imminent, so nothing later in
    /// the order can respond to it.
    /// </para>
    /// </remarks>
    private static void Soak(AbilityNode node, Cast cast)
    {
        var onto = Find(node.Require("onto"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would soak damage onto a card that is not there");

        long before = onto.Damage;
        onto.TakeDamage(cast.Incoming);
        cast.Events.Add(new FieldSet(onto.ObjectId, "k_damage", before, onto.Damage)
        {
            Trigger = cast.Trigger, Verb = "Place_Damage",
        });

        cast.Replace(0);
    }

    /// <summary>"Exhaust …" — <c>rr:exhausted</c>.</summary>
    /// <remarks>
    /// A card already exhausted stays exhausted and reports nothing:
    /// <c>rr:exhausted</c> is a state and not a counter, so exhausting
    /// twice is not two exhaustions and must not be two events on the wire.
    /// </remarks>
    private static void Exhaust(AbilityNode node, Cast cast)
    {
        var target = Find(node.Argument, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would exhaust a card that is not there");

        if (!target.Ready)
        {
            return;
        }

        target.Exhaust();
        cast.Events.Add(new FieldSet(target.ObjectId, "is_exhaust", 0, 1)
        {
            Trigger = cast.Trigger, Verb = "Exhaust",
        });
    }

    /// <summary>
    /// "Reveal the top card of the encounter deck" — <c>rr:reveal</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Revealed, not dealt.</b> <c>rr:deal-deal-an-encounter-card</c> puts a
    /// card facedown in a queue to be resolved later; this one is turned over
    /// now. The difference is a whole villain phase, and Under Fire says
    /// "reveal".
    /// </para>
    /// <para>
    /// Scheduled, for the same reason <c>search</c> schedules: revealing an
    /// encounter card is a step with an interrupt window and a response window
    /// around it, and the card revealed may itself ask a player something.
    /// </para>
    /// <para>
    /// <c>EncounterDeck.TakeTop</c> is what draws it, so an empty deck
    /// reshuffles its discard pile first — <c>rr:encounter-deck.3</c> — rather
    /// than this quietly doing nothing.
    /// </para>
    /// </remarks>
    private static Card? TopOfTheEncounterDeck(Cast cast) =>
        EncounterDeck.TakeTop(cast.World, cast.Trigger, cast.Events);

    /// <summary>Reveals one card, wherever it was.</summary>
    /// <remarks>
    /// <b>The card moves now and resolves later.</b> It goes to the revealing
    /// area at once, so a later step of the same ability cannot find it where
    /// it was — Shadow of the Past reveals two cards out of a pile and then
    /// shuffles "the rest" of that pile away, and a reveal that only scheduled
    /// would shuffle the two it had just chosen.
    /// </remarks>
    private static void RevealCard(Card? card, Cast cast)
    {
        if (card is null)
        {
            return;
        }

        World.MoveToTop(card, cast.World.AreaOf(DeckType.RevealingArea));
        cast.World.Agenda.Then(new PhaseStep(
            Steps.RevealEncounterCard,
            cast.World.Agenda.Current?.Round ?? 0,
            4,
            Index: cast.Player,
            Subject: card.ObjectId,
            Seat: cast.Player));
    }

    /// <summary>
    /// "Shuffle the rest of … into the encounter deck" — <c>rr:shuffle</c>.
    /// </summary>
    /// <remarks>
    /// The cards move in the order the query answers and the deck is shuffled
    /// once afterwards, not once per card. The shuffle draws from the game's
    /// single random stream, so how many times it happens is a wire fact and
    /// not a detail.
    /// </remarks>
    private static void ShuffleInto(AbilityNode node, Cast cast)
    {
        var deck = Area(Word(node.Require("deck")), cast);
        foreach (var card in Every(node.Require("cards"), cast))
        {
            var from = card.Area;
            World.MoveToTop(card, deck);
            cast.Events.Add(new CardsMoved(
                Places.Reference(from), Places.Reference(deck),
                [new Landing(card.ObjectId, deck.Cards.Count - 1)])
            {
                Trigger = cast.Trigger, Verb = "Shuffle_Into",
            });
        }

        cast.World.Shuffle(deck);
    }

    /// <summary>
    /// "Search the encounter deck and discard pile for … and reveal it" —
    /// <c>rr:search</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:search.2</c> — "cards being searched are not considered to leave
    /// the searched area" — so looking costs nothing and only the card found
    /// moves.
    /// </para>
    /// <para>
    /// <b>The reveal is scheduled, not done here.</b> Revealing an encounter
    /// card is a step with an interrupt window and a response window around it,
    /// and a reveal called inline would have neither. The step is the same one
    /// the villain phase uses, so the card found goes through
    /// <c>rr:reveal</c>'s four steps exactly as a dealt card does.
    /// </para>
    /// <para>
    /// <c>rr:search.3</c> — "if any portion of a deck is searched, upon
    /// completion of that game step, game function, or card ability, shuffle
    /// that entire deck." Taken as the ability completing, which is this method
    /// returning; the reveal it scheduled happens afterwards. Nothing in the
    /// pool that is reached this way reads the encounter deck, so the two
    /// readings agree on every board that exists — but this is the one written
    /// down.
    /// </para>
    /// <para>
    /// <c>rr:search.1</c> gives the player the choice when several cards match.
    /// That is a second suspension inside an ability that may already have one,
    /// so it is refused by name until a card needs it.
    /// </para>
    /// </remarks>
    private static void Search(AbilityNode node, Cast cast)
    {
        string wanted = Word(node.Require("for"));
        var searched = Nodes(node.Require("in")).Select(where => where.Kind).ToList();
        var areas = searched.Select(where => Area(where, cast)).ToList();

        var found = areas
            .SelectMany(area => area.Cards)
            .Where(card => string.Equals(card.FaceId, wanted, StringComparison.Ordinal))
            .ToList();

        if (found.Count > 1)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' searched and found {found.Count} copies of "
                + $"'{wanted}'; rr:search.1 gives the player that choice and asking is "
                + "not implemented");
        }

        if (found.Count == 1)
        {
            cast.World.Agenda.Then(new PhaseStep(
                Steps.RevealEncounterCard,
                cast.World.Agenda.Current?.Round ?? 0,
                4,
                Index: cast.Player,
                Subject: found[0].ObjectId,
                Seat: cast.Player));
        }

        cast.Results["found"] = found.Count;

        // `rr:search.3`. The discard pile is not a deck and is not shuffled --
        // and shuffling one would consume from the game's single random stream,
        // which is a wire format.
        foreach (var deck in areas.Where(area => area.Type == DeckType.EncounterDeck))
        {
            cast.World.Shuffle(deck);
        }
    }

    /// <summary>Which place on the board a word names.</summary>
    private static Area Area(string where, Cast cast) => where switch
    {
        "encounterDeck" => cast.World.AreaOf(DeckType.EncounterDeck),
        "encounterDiscardPile" => cast.World.AreaOf(DeckType.EncounterDiscardPile),
        _ => throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' searches '{where}', which is not implemented"),
    };

    /// <summary>
    /// "Choose to either … or …" — <c>rr:choose-option</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The ability stops here.</b> An interpreter that returns a list of
    /// events has nowhere to ask a question, so the choice becomes a step on
    /// the agenda and what resumes the ability is the answer to it. The step
    /// carries the source card and the seat; <see cref="Choice"/> finds the
    /// node again from the card, which is why an ability may hold only one.
    /// </para>
    /// <para>
    /// <c>rr:choose-game-element.1</c> settles who is asked, and it is the
    /// player resolving the ability — not the first player, and not the card's
    /// owner, which an encounter card has not got.
    /// </para>
    /// </remarks>
    /// <summary>
    /// The steps of a <c>seq</c>, from wherever the ability left off.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An ability can ask more than once.</b> Eviction Notice says "you may
    /// flip to alter-ego form" and then "choose:", which is two questions in a
    /// row; 36 cards in the pool pair a "may" with a listed choice, and every
    /// "may" is itself a question.
    /// </para>
    /// <para>
    /// So a suspended ability remembers <i>where</i>, and that place is an
    /// index into the top-level sequence — one number, which is what a
    /// <see cref="PhaseStep"/> can carry and what survives a save. A choice
    /// nested inside an <c>if</c> inside a <c>seq</c> is refused by name
    /// instead; nothing in the pool needs one, and inventing a path notation
    /// for it would be inventing the general case for no card.
    /// </para>
    /// </remarks>
    private static void Sequence(AbilityNode node, Cast cast, int from)
    {
        var steps = Nodes(node.Argument).ToList();
        for (int step = from; step < steps.Count; step++)
        {
            cast.At(step);
            Run(steps[step], cast);
            if (cast.Suspended)
            {
                return;
            }
        }
    }

    private static void Choose(AbilityNode node, Cast cast)
    {
        if (node.Kind == "choose" && Nodes(node.Require("options")).Count() < 2)
        {
            throw new AbilityException(
                $"'{cast.Source.FaceId}' offers a choice of one, which is not a choice");
        }

        if (node.Kind == "chooseCard" && Every(node.Require("from"), cast).Count == 0)
        {
            // `rr:choose-game-element` chooses "a game element that meets the
            // specific requirements of an ability", and here there is none.
            // Nothing to ask, so the card must have said what happens instead
            // -- Caught Off Guard's surge is in the branch that would have got
            // here, not after the choice.
            throw new AbilityException(
                $"'{cast.Source.FaceId}' would choose a card and there is none to choose; "
                + "guard the choice with `exists`");
        }

        // `Index` is where to pick the ability up: the step *after* this
        // choice in the top-level sequence. A choice that is the whole effect
        // has nothing after it and resumes at one, which runs nothing.
        cast.World.Agenda.Then(new PhaseStep(
            Steps.ChooseOption,
            cast.World.Agenda.Current?.Round ?? 0,
            2,
            Index: cast.Position + 1,
            Subject: cast.Source.ObjectId,
            Seat: cast.Player));

        cast.Suspend();
    }

    /// <summary>"… heals N damage" — <c>rr:heal</c>.</summary>
    /// <remarks>
    /// <para>
    /// What it records is the point. <c>rr:heal</c> heals up to the amount, and
    /// a character at full health or damaged by less heals less than it was
    /// told to — so <c>result.healed</c> is what actually moved, and a card
    /// reading "if no damage was healed this way" reads that rather than
    /// checking the character's health first. The check <i>before</i> is
    /// silently wrong: it reads a number the heal may never reach.
    /// </para>
    /// <para>
    /// A target that is not on the board heals nothing rather than throwing.
    /// "Rhino heals 4 damage. If no damage was healed this way, this card gains
    /// surge" is a sentence with an answer for the absent villain, and it is
    /// the surge.
    /// </para>
    /// </remarks>
    private static void Heal(AbilityNode node, Cast cast)
    {
        long healed = Find(node.Require("card"), cast) is { } target
            ? Damage.Heal(
                cast.World, cast.World.Facts, target, Amount(node.Require("amount"), cast),
                cast.Trigger, "Heal", cast.Events)
            : 0;

        cast.Results["healed"] = healed;
    }

    /// <summary>
    /// "Assign N damage among …" — <c>rr:indirect-damage</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>.1</c>: "indirect damage dealt to a player can be divided as that
    /// player chooses among characters under their control." <c>.2</c> is the
    /// group form, "among friendly characters in play", which is what "assign X
    /// damage among heroes and allies" means.
    /// </para>
    /// <para>
    /// <b>Only asked when there is something to ask.</b> A player with no ally
    /// has one character, so every point goes to their identity and there is no
    /// division to choose — which is most of the 101 cards in the pool that
    /// deal indirect damage. It suspends only when the eligible characters can
    /// hold the damage more than one way.
    /// </para>
    /// <para>
    /// <c>.3.1</c> caps each character at its remaining hit points: "a
    /// character cannot be assigned more indirect damage than would cause it to
    /// be defeated", assessed "without accounting for interactions with other
    /// abilities". <c>.3.2</c> keeps a tough character eligible up to that same
    /// cap even though the tough card will prevent all of it, and <c>.3</c>
    /// assigns everything before resolving any of it.
    /// </para>
    /// </remarks>
    private static void Indirect(AbilityNode node, Cast cast)
    {
        long amount = Amount(node.Require("amount"), cast);
        var eligible = Assignable(node.Require("among"), cast);

        if (amount <= 0 || eligible.Count == 0)
        {
            return;
        }

        if (eligible.Count == 1)
        {
            // No division to choose. `.3.1`'s cap still applies -- a character
            // cannot be assigned more than would defeat it -- so what is over
            // the cap is simply not assigned.
            Assign(cast, [eligible[0]], amount);
            return;
        }

        cast.World.Agenda.Then(new PhaseStep(
            Steps.ChooseOption,
            cast.World.Agenda.Current?.Round ?? 0,
            2,
            Index: cast.Position + 1,
            Subject: cast.Source.ObjectId,
            Seat: cast.Player));

        cast.Suspend();
    }

    /// <summary>The characters indirect damage may be assigned to.</summary>
    /// <remarks>
    /// <c>rr:indirect-damage.4</c>: "characters that cannot take damage cannot
    /// be assigned indirect damage", and <c>.3.1</c> makes a character with no
    /// hit points left ineligible for the same reason — there is no amount that
    /// would not defeat it.
    /// </remarks>
    private static List<Card> Assignable(AbilityValue among, Cast cast) =>
    [
        .. Every(among, cast).Where(card => Room(cast, card) > 0),
    ];

    /// <summary>How much indirect damage one character may be assigned.</summary>
    private static long Room(Cast cast, Card card) =>
        Damage.Health(cast.World, cast.World.Facts, card) - card.Damage;

    /// <summary>Assigns the damage, then resolves it — <c>rr:indirect-damage.3</c>.</summary>
    /// <remarks>
    /// "All indirect damage from a single source is <b>first assigned and then
    /// resolved simultaneously</b>." So the whole assignment is worked out
    /// before any of it is dealt, which is what stops the first point defeating
    /// a character and making the rest illegal.
    /// </remarks>
    private static void Assign(Cast cast, IReadOnlyList<Card> among, long amount)
    {
        var assigned = new Dictionary<int, long>();
        long left = amount;

        foreach (var card in among)
        {
            if (left <= 0)
            {
                break;
            }

            long take = Math.Min(Room(cast, card), left);
            if (take <= 0)
            {
                continue;
            }

            assigned[card.ObjectId] = take;
            left -= take;
        }

        Resolve(cast, assigned);
    }

    /// <summary>Deals an assignment that is already worked out.</summary>
    /// <remarks>
    /// In object-id order, because <c>rr:indirect-damage.3</c> resolves it
    /// "simultaneously" and simultaneous still has to reach the event stream in
    /// some order — one the board cannot see and the wire can.
    /// </remarks>
    private static void Resolve(Cast cast, Dictionary<int, long> assigned)
    {
        foreach (var (card, damage) in assigned.OrderBy(each => each.Key))
        {
            Damage.Deal(
                cast.World, cast.World.Facts, cast.World.Cards[card], damage,
                cast.Trigger, "Indirect_Damage", cast.Events);
        }
    }

    /// <summary>"Deal N damage to …" — <c>rr:damage</c>.</summary>
    /// <remarks>
    /// Through <see cref="Damage.Deal"/> and not at the token, because damage
    /// is one rule however it arrived: <c>rr:tough.2</c> prevents all of it and
    /// discards a status card instead, and <c>rr:defeat</c> is the other half
    /// of the same moment. A card that wrote to <c>k_damage</c> would skip
    /// both and leave a defeated character standing.
    /// </remarks>
    private static void DealDamage(AbilityNode node, Cast cast)
    {
        long amount = Amount(node.Require("amount"), cast);
        foreach (var target in Every(node.Require("cards"), cast))
        {
            Damage.Deal(
                cast.World, cast.World.Facts, target, amount, cast.Trigger, "Deal_Damage",
                cast.Events);
        }
    }

    /// <summary>"Place N threat on …" — <c>rr:threat</c>.</summary>
    /// <remarks>
    /// Through <see cref="Threat.Place"/>, which checks
    /// <c>rr:main-scheme-main-scheme-deck.2</c> afterwards: threat that reaches
    /// a main scheme's target completes it whatever put it there, and a card
    /// placing threat is one of the things that can.
    /// </remarks>
    private static void PlaceThreat(AbilityNode node, Cast cast)
    {
        // "On each side scheme" and "here" are the same node with a different
        // query: `Every` answers one card or many, so a card that names one
        // scheme and a card that names all of them read alike.
        var schemes = Every(node.Require("scheme"), cast);
        if (schemes.Count == 0)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would place threat on a scheme that is not there");
        }

        long amount = Amount(node.Require("amount"), cast);
        foreach (var scheme in schemes)
        {
            Threat.Place(
                cast.World, cast.World.Facts, cast.Abilities, scheme, amount,
                cast.Trigger, cast.Events);
        }
    }

    /// <summary>
    /// "Each player places a random card from their hand facedown here."
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Placed, not discarded.</b> The card is still a card and comes back —
    /// Highway Robbery's "When Defeated" returns each one to its owner's hand.
    /// So it goes onto the host as an attachment, which is what
    /// <c>rr:attachment</c> makes "here" mean, and it goes <b>facedown</b>:
    /// nobody may look at it while it is there.
    /// </para>
    /// <para>
    /// One draw from the game's single random stream per card taken, in player
    /// order, for the same reason <c>discardAtRandom</c> takes them that way —
    /// the order is what the stream sees.
    /// </para>
    /// </remarks>
    private static void PlaceAtRandom(AbilityNode node, Cast cast)
    {
        var host = Find(node.Require("on"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' places cards on a card that is not there");

        var onto = cast.World.AreaOf(
            DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId, host.Area.CardOwner);
        long count = Amount(node.Require("count"), cast);

        foreach (int seat in Seats(node.Require("player"), cast))
        {
            var hand = cast.World.Seats[seat].Hand;
            for (long placed = 0; placed < count && hand.Cards.Count > 0; placed++)
            {
                var card = cast.World.Random.Choice(hand.Cards);
                var from = card.Area;
                World.MoveToTop(card, onto);
                card.TurnFaceDown();

                cast.Events.Add(new CardsMoved(
                    Places.Reference(from), Places.Reference(onto),
                    [new Landing(card.ObjectId, onto.Cards.Count - 1)])
                {
                    Trigger = cast.Trigger, Verb = "Place",
                });
                cast.Events.Add(new CardAttached(card.ObjectId, host.ObjectId)
                {
                    Trigger = cast.Trigger, Verb = "Place",
                });
            }
        }
    }

    /// <summary>"Return each … to its owner's hand."</summary>
    /// <remarks>
    /// To <b>its owner's</b> hand and not the resolving player's: a card placed
    /// by each player comes back to each player. Ownership is the card's, which
    /// is why <c>Card.Owner</c> decides rather than whoever defeated the
    /// scheme.
    /// </remarks>
    private static void ReturnToHand(AbilityNode node, Cast cast)
    {
        foreach (var card in Every(node.Argument, cast))
        {
            var from = card.Area;
            var hand = cast.World.Seats[card.Owner].Hand;
            World.MoveToTop(card, hand);
            card.TurnFaceUp();

            cast.Events.Add(new CardsMoved(
                Places.Reference(from), Places.Reference(hand),
                [new Landing(card.ObjectId, hand.Cards.Count - 1)])
            {
                Trigger = cast.Trigger, Verb = "Return",
            });
            cast.Events.Add(new CardDetached(card.ObjectId, from.Host)
            {
                Trigger = cast.Trigger, Verb = "Return",
            });
        }
    }

    /// <summary>
    /// "Discard N cards at random from … hand".
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The draw is a wire format.</b> One MT19937 stream runs the whole
    /// game, so how many numbers this takes and in what order decides every
    /// later shuffle and every later random card. <c>EngineRandom.Choice</c> is
    /// the ported primitive and is pinned against recorded RNG vectors; this
    /// takes one draw per card discarded, from the hand as it stands after the
    /// previous one.
    /// </para>
    /// <para>
    /// "From <b>each</b> player's hand" goes in player order —
    /// <c>rr:in-player-order</c> — because the order is what the stream sees.
    /// A player with an empty hand discards nothing and takes no draw.
    /// </para>
    /// <para>
    /// What it records is <c>result.resourceTypes</c>: how many <i>different</i>
    /// resource types went, which is what "for each different resource type
    /// discarded this way" counts. A card printing two of one letter is one
    /// type, and a card printing none is none.
    /// </para>
    /// </remarks>
    private static void DiscardAtRandom(AbilityNode node, Cast cast)
    {
        long count = Amount(node.Require("count"), cast);
        var types = new SortedSet<char>();
        long discarded = 0;

        foreach (int seat in Seats(node.Require("player"), cast))
        {
            var hand = cast.World.Seats[seat].Hand;
            for (long gone = 0; gone < count && hand.Cards.Count > 0; gone++)
            {
                var card = cast.World.Random.Choice(hand.Cards);
                types.UnionWith(Resources.GeneratedBy(card.FaceId, cast.World.Facts));
                Marvel.Rules.Play.Discard.Card(cast.World, card, cast.Trigger, cast.Events);
                discarded += 1;
            }
        }

        cast.Results["discarded"] = discarded;
        cast.Results["resourceTypes"] = types.Count;
    }

    /// <summary>Which seats a word names.</summary>
    /// <remarks>
    /// <c>rr:each-player.1</c> resolves "each player" in player order when the
    /// effect does not say otherwise, and <c>rr:player-elimination.6</c> is why
    /// that is <c>PlayerOrder</c>: "effects that refer to the players in the
    /// game ignore eliminated players".
    /// </remarks>
    private static IEnumerable<int> Seats(AbilityValue value, Cast cast) =>
        Word(value) switch
        {
            "you" => [cast.Player],
            "each" => cast.World.PlayerOrder,
            _ => throw new AbilityException(
                $"'{Word(value)}' does not name a set of players"),
        };

    /// <summary>
    /// "Discard cards from the top of the encounter deck until a … is
    /// discarded" — <c>rr:discard.4</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "If multiple cards are discarded from a deck by a singular effect, place
    /// those cards in the appropriate discard pile <b>one at a time (without
    /// changing the order)</b>", and <c>.4.1</c> makes them simultaneous all
    /// the same. So this takes the top card each time rather than counting
    /// ahead — and through <see cref="EncounterDeck.TakeTop"/>, so a deck that
    /// empties mid-search reshuffles instead of ending the search.
    /// </para>
    /// <para>
    /// <b>Bounded, and the bound is a rule and not a fear.</b> A search for a
    /// card that is in neither the deck nor the discard pile would otherwise
    /// reshuffle for ever. The bound is how many cards there are, so a card
    /// that exists is always found and one that does not ends the search
    /// instead of the game.
    /// </para>
    /// </remarks>
    private static void DiscardUntil(AbilityNode node, Cast cast)
    {
        var deck = Area(Word(node.Require("from")), cast);
        var wanted = Kind(Word(node.Require("kind")));

        long bound = deck.Cards.Count
            + cast.World.AreaOf(DeckType.EncounterDiscardPile).Cards.Count;

        for (long looked = 0; looked < bound; looked++)
        {
            var card = EncounterDeck.TakeTop(cast.World, cast.Trigger, cast.Events);
            if (card is null)
            {
                return;
            }

            Marvel.Rules.Play.Discard.Card(cast.World, card, cast.Trigger, cast.Events);
            if (cast.World.Facts.Kind(card.FaceId) == wanted)
            {
                RevealCard(card, cast);
                return;
            }
        }
    }

    /// <summary>Which card type a word names.</summary>
    private static CardKind Kind(string named) => named switch
    {
        "sideScheme" => CardKind.EncounterSideScheme,
        "minion" => CardKind.Minion,
        _ => throw new RulesNotImplementedException(
            $"'{named}' is not a card type this engine can search for"),
    };

    /// <summary>
    /// "The villain attacks you", "the villain schemes" — an enemy activation
    /// a card asked for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Scheduled, not called.</b> <c>rr:attack-enemy-activation</c> is six
    /// steps and one of them asks a player who is defending, so an activation
    /// cannot resolve inside an ability that has to return. It goes on the
    /// agenda, and <c>Agenda.Then</c> puts it after the step that is running —
    /// which is what <c>rr:surge.2</c> wants anyway: finish resolving the card
    /// before what it caused happens.
    /// </para>
    /// <para>
    /// <b>Which activation is the card's to say.</b> <c>rr:activation.1</c>
    /// reads it off the player's form — attack in hero form, scheme in
    /// alter-ego form — but that rule is about the activation the villain phase
    /// schedules. A card that says "the villain attacks you" has already
    /// chosen, and reading the form here would make Assault do nothing to a
    /// hero who had flipped since the card was dealt.
    /// </para>
    /// <para>
    /// One step per enemy, in the order <see cref="Every"/> returns them.
    /// <c>rr:minion.3</c> makes that order the player's choice; it is taken
    /// here as the order the minions sit in the play area, deterministically
    /// and stated, exactly as the villain phase's own step 2 takes it.
    /// </para>
    /// </remarks>
    private static void Activate(AbilityNode node, Cast cast, string what)
    {
        // Against the player resolving the card. Every printed card that causes
        // an activation says "you", and `rr:reveal.2` makes that the revealing
        // player -- so there is no field here to name somebody else, and a card
        // that names one grows the vocabulary then rather than leaving an
        // untaken branch now.
        int seat = cast.Player;

        // The round the activation belongs to is the round the card was
        // revealed in. Nothing else on the agenda can tell it.
        int round = cast.World.Agenda.Current?.Round ?? 0;

        foreach (var enemy in Every(node.Require("enemies"), cast))
        {
            cast.World.Agenda.Then(new PhaseStep(
                what, round, 2, Index: seat, Subject: enemy.ObjectId, Seat: seat));
        }
    }

    /// <summary>Every card a value names, which may be none.</summary>
    /// <remarks>
    /// A value that names one card answers with that one, so a card reading
    /// "the villain attacks you" and one reading "each minion engaged with you
    /// attacks you" are the same node with a different argument.
    /// </remarks>
    private static IReadOnlyList<Card> Every(AbilityValue value, Cast cast)
    {
        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } query
            && query.Argument is AbilityValue.Word { Value: "minionsEngagedWithYou" })
        {
            // `rr:engage.1` -- "when a minion engages a player, it is placed in
            // that player's play area". Engagement *is* which area the minion
            // sits in, so this is a read of the board and not of a flag; and
            // "you" is the player resolving the card, so a minion engaged with
            // somebody else is not in this list however close it is on the
            // table.
            return [.. cast.World
                .AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(cast.Player))
                .Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } attached
            && attached.Argument is AbilityValue.Word { Value: "attachedToThis" })
        {
            // What is sitting on this card. `rr:attachment` puts an attachment
            // in an area hosted by the card it is attached to, so this is a
            // read of the board.
            return
            [
                .. cast.World.Areas
                    .Where(area => area.Host == cast.Source.ObjectId)
                    .SelectMany(area => area.Cards),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } friendly
            && friendly.Argument is AbilityValue.Word { Value: "heroesAndAllies" })
        {
            // `rr:indirect-damage.2`'s "friendly characters in play", which
            // `rr:friendly` makes every player's rather than one player's: "a
            // blanket term that refers to cards **the players** control".
            //
            // **Every identity, not only those in hero form.** "Heroes and
            // allies" is what the card says, but `rr:you-your.3` divides
            // indirect damage "among characters in play under their control",
            // and a player in alter-ego form is still a character with hit
            // points. A reading that skipped them would leave damage
            // unassignable at a table where everyone had flipped down.
            return
            [
                .. cast.World.PlayerOrder.Select(seat => cast.World.Seats[seat].IdentityCard),
                .. cast.World.Areas
                    .Where(area => area.Type == DeckType.AlliesArea)
                    .SelectMany(area => area.Cards),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } schemes
            && schemes.Argument is AbilityValue.Word { Value: "sideSchemes" })
        {
            // "Each side scheme", which reaches the players' as well as the
            // scenario's: `rr:player-side-scheme` calls them "the player card
            // equivalent of the side schemes found in the encounter deck" and
            // `.1` puts them in the same place, next to the main scheme.
            return [.. cast.World.AreaOf(DeckType.SideSchemesArea).Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } pile
            && pile.Argument is AbilityValue.Word { Value: "yourAsidePile" })
        {
            // "The rest of your set-aside nemesis encounter set" -- whatever is
            // still in the pile once the cards this ability took out of it have
            // gone. The obligation is not among them: setup shuffles it into
            // the encounter deck long before this resolves.
            return [.. cast.World.Seats[cast.Player].Nemesis.Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } yours
            && yours.Argument is AbilityValue.Word { Value: "upgradesAndSupportsYouControl" })
        {
            // "An upgrade or support **you control**." A player's upgrades and
            // supports sit in their own play area, so control is where the card
            // is -- the same reading `rr:engage.1` gets for a minion.
            return
            [
                .. Owned.SelectMany(where =>
                    cast.World.AreaOf(where, PlayArea.Of(cast.Player)).Cards),
            ];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "minBy" or "maxBy" } ranked)
        {
            return Ranked(ranked, cast);
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } upgrades
            && upgrades.Argument is AbilityValue.Word { Value: "upgradesYouControl" })
        {
            // The upgrade half of `upgradesAndSupportsYouControl`, on its own,
            // because Beetle's two abilities both say "upgrade" and a support
            // is not one. Same reading of control: `rr:play-area.1` puts "any
            // cards in play under their control" in a player's own play area.
            return [.. cast.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(cast.Player)).Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } allies
            && allies.Argument is AbilityValue.Word { Value: "alliesYouControl" })
        {
            // "Each ally **you control**", which is where the card is:
            // `rr:play-area.1` puts "any cards in play under their control" in
            // a player's own play area, so control is a read of the board
            // rather than a field -- the same reading `rr:engage.1` gets for a
            // minion. Not `heroesAndAllies`, which is every player's: Boomerang
            // hits the allies of the player it attacked and nobody else's.
            return [.. cast.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(cast.Player)).Cards];
        }

        if (value is AbilityValue.Map && Tree(value) is { Kind: "query" } heroes
            && heroes.Argument is AbilityValue.Word { Value: "heroes" })
        {
            // **Not every identity.** `rr:form-change-form.5`: "while a player
            // is in alter-ego form, card abilities that interact with their
            // hero do not interact with their identity." So "each hero" passes
            // over a player who has flipped down, and Shocker's one damage is
            // one damage to whoever is standing up.
            return [.. cast.World.PlayerOrder
                .Select(seat => cast.World.Seats[seat])
                .Where(seat => Forms.In(cast.World, seat, cast.World.Facts, Forms.Hero))
                .Select(seat => seat.IdentityCard)];
        }

        return Find(value, cast) is { } one ? [one] : [];
    }

    /// <summary>
    /// "The lowest-cost upgrade you control" — <c>minBy</c> and <c>maxBy</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Ties are kept.</b> The Rules Reference gives no tie-break for "the
    /// lowest-cost X", and collapsing one here would be the interpreter
    /// deciding something the rules leave to the table. So this answers with
    /// every card that shares the extreme value, and the card that wants one
    /// wraps it in a <c>chooseCard</c> — which is where
    /// <c>rr:choose-game-element.1</c> puts the question, to the player
    /// resolving.
    /// </para>
    /// <para>
    /// <b>Permanents are not among the candidates.</b>
    /// <c>rr:permanent.4.1</c> names this exact shape: "if a permanent card
    /// would be targeted by such an effect <i>(for example, 'discard the
    /// lowest-cost support you control')</i>, that effect instead targets the
    /// <b>non-permanent</b> card that fits its criteria." So a permanent is
    /// dropped before the comparison rather than after it, or a cheap
    /// permanent would shield a dearer card that the effect should have taken.
    /// </para>
    /// </remarks>
    private static IReadOnlyList<Card> Ranked(AbilityNode node, Cast cast)
    {
        // Through `StateFields` rather than straight at the printed field:
        // `rr:permanent.1` makes the keyword "equivalent to the following
        // constant ability", and a constant ability is something a card can
        // grant. Reading print alone would miss a permanence handed out in
        // play.
        var among = Every(node.Require("of"), cast)
            .Where(card => StateFields.Modified(
                cast.World, card, "permanent", cast.World.Facts, cast.World.Players) <= 0)
            .ToList();

        if (among.Count == 0)
        {
            return [];
        }

        string key = Word(node.Require("by"));
        long Rank(Card card) => key switch
        {
            // `rr:dash-value.3` -- a printed dash "is treated as an
            // unmodifiable 0", which is what `PrintedValue` answers for a field
            // that is not a number, so nothing extra is needed for it here.
            "cost" => cast.World.Facts.PrintedValue(card.FaceId, "Cost", cast.World.Players),
            "attack" => StateFields.Modified(
                cast.World, card, "attack", cast.World.Facts, cast.World.Players),
            _ => throw new AbilityException($"'{key}' is not a value cards can be ranked by"),
        };

        long extreme = node.Kind == "minBy" ? among.Min(Rank) : among.Max(Rank);
        return [.. among.Where(card => Rank(card) == extreme)];
    }

    /// <summary>Which card a value names, or null when it names none.</summary>
    private static Card? Find(AbilityValue value, Cast cast) => value switch
    {
        AbilityValue.Word word => Named(word.Value, cast),
        AbilityValue.Map => Query(Tree(value), cast),
        _ => throw new AbilityException($"{AbilityNode.Describe(value)} does not name a card"),
    };

    private static Card? Named(string name, Cast cast) => name switch
    {
        "this" => cast.Source,

        // The card a `chooseCard` was answered with. Null while the ability is
        // still asking, which is why nothing before the answer can read it.
        "chosen" => cast.Chosen,

        // "Your hero" and not "you". `rr:form-change-form.5`: "while a player
        // is in alter-ego form, card abilities that interact with their hero do
        // not interact with their identity" -- so this names nothing at all
        // when the player has flipped down, and a card that has something to
        // say about that says it with `exists`.
        "yourHero" => Forms.In(
            cast.World, cast.World.Seats[Resolver(cast)], cast.World.Facts, Forms.Hero)
            ? cast.World.Seats[Resolver(cast)].IdentityCard
            : null,

        // `rr:you-your.5`: "if a card ability places a status card on 'you'
        // (such as 'you are stunned'), the player resolving that card ability
        // places that status card on their identity." `rr:you-your` opens with
        // the general form -- "if the word 'you' **can** be resolved as
        // referring to the player's identity, it **must** be resolved as such"
        // -- so "you" is a card here whenever a card is what is wanted.
        "you" => cast.World.Seats[Resolver(cast)].IdentityCard,
        "attachedTo" => cast.Source.Area.Host >= 0 ? cast.World.Cards[cast.Source.Area.Host] : null,
        "trigger.subject" => cast.Occurrence.Subject >= 0
            ? cast.World.Cards[cast.Occurrence.Subject]
            : null,
        _ => throw new AbilityException($"'{name}' does not name a card"),
    };

    private static Card? Query(AbilityNode node, Cast cast)
    {
        // "Bomb Scare", "Vulture" -- a card in play named by its title, which
        // is a query with an argument rather than one of the bare words below.
        // `rr:identity.2` makes a title name one card, so this compares titles
        // and not printed ids.
        if (node.Kind == "titled")
        {
            return cast.World.Areas
                .Where(area => DeckTypes.IsInPlay(area.Type))
                .SelectMany(area => area.Cards)
                .FirstOrDefault(card => string.Equals(
                    cast.World.Facts.Title(card.FaceId), Word(node.Argument),
                    StringComparison.Ordinal));
        }

        if (node.Kind != "query")
        {
            throw new AbilityException($"'{node.Kind}' does not name a card");
        }

        string what = Word(node.Argument);
        return what switch
        {
            // `rr:villain-villain-deck` -- one villain is in the villain area.
            "villain" => cast.World.TheCardIn(DeckType.VillainArea),
            "mainScheme" => cast.World.TheCardIn(DeckType.MainSchemesArea),

            // "Your set-aside nemesis minion" and "your set-aside nemesis side
            // scheme". A nemesis set holds one of each, so naming the kind
            // names the card -- and answering null when it has already been
            // taken is what Shadow of the Past's surge branch reads.
            "yourAsideMinion" => Aside(cast, CardKind.Minion),
            "yourAsideSideScheme" => Aside(cast, CardKind.EncounterSideScheme),
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' queries '{what}', which is not implemented"),
        };
    }

    /// <summary>The one card of a kind in the player's set-aside pile.</summary>
    private static Card? Aside(Cast cast, CardKind kind) =>
        cast.World.Seats[cast.Player].Nemesis.Cards
            .FirstOrDefault(card => cast.World.Facts.Kind(card.FaceId) == kind);

    /// <summary>
    /// Which player is resolving this ability, or a refusal.
    /// </summary>
    /// <remarks>
    /// <b>An encounter card's ability does not always have one.</b> A "When
    /// Defeated" on a minion belongs to nobody until somebody defeats it, and
    /// the cards say whose it is themselves — "the player who defeated Fabian
    /// Cortez". Until <c>Defeat</c> carries that, a card that asks for a player
    /// it has not got is refused by name rather than reaching for the first
    /// one.
    /// </remarks>
    private static int Resolver(Cast cast) => cast.Player >= 0
        ? cast.Player
        : throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' asks who is resolving it, and an encounter card's "
            + "ability has no player unless the card says which");

    private static int Seat(AbilityValue value, Cast cast) =>
        value is AbilityValue.Word word
            ? word.Value switch
            {
                "trigger.player" => cast.Occurrence.Player,
                "you" => Resolver(cast),
                "controller" => cast.Source.Owner,
                _ => throw new AbilityException($"'{word.Value}' does not name a player"),
            }
            : throw new AbilityException(
                $"{AbilityNode.Describe(value)} does not name a player");

    private static IEnumerable<AbilityNode> Nodes(AbilityValue value) =>
        value is AbilityValue.List list
            ? list.Values.Select(Tree)
            : throw new AbilityException(
                $"{AbilityNode.Describe(value)} is not a list of nodes");

    private static AbilityNode Tree(AbilityValue value) => AbilityNode.Of(value);

    private static string Word(AbilityValue value) =>
        value is AbilityValue.Word word
            ? word.Value
            : throw new AbilityException($"{AbilityNode.Describe(value)} is not a word");

    /// <summary>How much, which may be printed per player.</summary>
    /// <remarks>
    /// <c>rr:per-player-icon</c> multiplies by the number of players, and
    /// <c>rr:player-elimination.6</c> is the exception that keeps this
    /// <c>World.Players</c> rather than the number still playing: "effects that
    /// refer to the players in the game ignore eliminated players, <b>except
    /// for the per player icon</b>."
    /// </remarks>
    private static long Amount(AbilityValue value, Cast cast)
    {
        if (value is not AbilityValue.Map)
        {
            return Number(value);
        }

        var node = Tree(value);
        return node.Kind switch
        {
            "perPlayer" => Number(node.Argument) * cast.World.Players,

            // "X is the amount of threat on Bomb Scare" -- a number read off
            // the board rather than printed. `rr:threat` counts tokens, so this
            // is the token pool and not a printed field.
            "tokensOn" => Find(node.Argument, cast) is { } holder
                ? holder.Tokens.GetValueOrDefault("k_threat")
                : 0,

            // `result.*` -- what an action earlier in this ability actually
            // did, which is not what it was asked to do. Zero when nothing has
            // written it, so a card reading a result it never produced reads a
            // number rather than throwing: "no damage was healed" is exactly
            // the case where nothing ran.
            "result" => cast.Results.GetValueOrDefault(Word(node.Argument)),

            // "If there is at least 5 damage here" -- damage tokens on a card,
            // which `rr:damage.2` puts on an ally or minion and which an
            // attachment can hold when a card puts them there.
            "damageOn" => Find(node.Argument, cast)?.Damage ?? 0,
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' asks for the amount '{node.Kind}', "
                + "which is not implemented"),
        };
    }

    private static long Number(AbilityValue value) =>
        value is AbilityValue.Number number
            ? number.Value
            : throw new AbilityException($"{AbilityNode.Describe(value)} is not a number");

    /// <summary>What one ability is resolving against.</summary>
    /// <param name="World">The board.</param>
    /// <param name="Source">The card whose text this is.</param>
    /// <param name="Occurrence">What it is timed to.</param>
    /// <param name="Player">The seat resolving it.</param>
    /// <param name="Events">Where to record what it did.</param>
    /// <param name="Abilities">
    /// The runner itself, for the rules that run more cards. A main scheme this
    /// ability completes advances, and <c>rr:villain-defeat</c> resolves the
    /// new stage's own "When Revealed" — so an ability can reach back into the
    /// interpreter that is running it.
    /// </param>
    private sealed record Cast(
        World World, Card Source, Occurrence Occurrence, int Player, List<GameEvent> Events,
        ICardAbilities Abilities)
    {
        /// <summary>The trigger string this ability's events carry.</summary>
        public string Trigger => Occurrence.Conditions[0];

        /// <summary>
        /// What the actions in this ability actually did — the <c>result.*</c>
        /// namespace.
        /// </summary>
        /// <remarks>
        /// Scoped to one resolution of one ability, because that is the scope
        /// the cards use: "if no damage was healed <b>this way</b>" is about
        /// this sentence and not about the game.
        /// </remarks>
        public Dictionary<string, long> Results { get; } = new(StringComparer.Ordinal);

        /// <summary>Whether this ability has stopped to ask a question.</summary>
        public bool Suspended { get; private set; }

        /// <summary>Stops the ability here — <c>rr:choose-option</c>.</summary>
        public void Suspend() => Suspended = true;

        /// <summary>Which step of the top-level sequence is running.</summary>
        public int Position { get; private set; }

        /// <summary>Records which step of the sequence this is.</summary>
        /// <param name="step">Its index.</param>
        public void At(int step) => Position = step;

        /// <summary>The card the player picked, once they have.</summary>
        public Card? Chosen { get; private set; }

        /// <summary>Records the card a <c>chooseCard</c> was answered with.</summary>
        /// <param name="card">What they picked.</param>
        public void Choose(Card card) => Chosen = card;

        /// <summary>How much damage is about to be dealt — <c>rr:damage.step.1</c>.</summary>
        public long Incoming { get; init; }

        /// <summary>How much is left after this ability, defaulting to all of it.</summary>
        public long Remaining { get; private set; } = -1;

        /// <summary>Replaces the damage with this much.</summary>
        /// <param name="amount">What is left.</param>
        public void Replace(long amount) => Remaining = amount;
    }
}
