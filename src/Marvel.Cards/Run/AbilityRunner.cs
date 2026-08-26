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
        var cast = new Cast(world, card, occurrence, ability.Player, events, this);

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
                string.Concat(CardPlay.Generators(world.Facts, world.Seats[player])
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
            Sources: CardPlay.Generators(world.Facts, world.Seats[player]));
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
                cast.Events);
            return;
        }

        Run(cost, cast);
    }

    /// <inheritdoc/>
    public Prompt? Choosing(World world, Card source, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);

        var choice = Choice(source);
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

    /// <inheritdoc/>
    public IReadOnlyList<GameEvent> Chose(World world, Card source, int player, Decision input)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(input);

        var choice = Choice(source);
        var cast = Resolving(world, source, player);

        if (choice.Kind == "chooseCard")
        {
            cast.Choose(
                Every(choice.Require("from"), cast)
                    .FirstOrDefault(card => card.ObjectId == input.Affordance)
                ?? throw new RulesNotImplementedException(
                    $"'{source.FaceId}' did not offer card {input.Affordance} to choose"));

            Run(Tree(choice.Require("effect")), cast);
            return cast.Events;
        }

        var options = Nodes(choice.Require("options")).ToList();
        if (input.IsDecline || input.Affordance < 0 || input.Affordance >= options.Count)
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' offers {options.Count} options and none of them is "
                + $"number {input.Affordance}");
        }

        Run(options[input.Affordance], cast);
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
    private AbilityNode Choice(Card source)
    {
        var found = book.On(source.FaceId)
            .SelectMany(ability => Choices(ability.Effect))
            .ToList();

        return found.Count == 1
            ? found[0]
            : throw new RulesNotImplementedException(
                $"'{source.FaceId}' holds {found.Count} choices, and exactly one can be "
                + "waiting on an answer");
    }

    /// <summary>Every <c>choose</c> node in one effect tree.</summary>
    private static IEnumerable<AbilityNode> Choices(AbilityNode node)
    {
        if (node.Kind is "choose" or "chooseCard")
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
                foreach (var step in Nodes(node.Argument))
                {
                    if (cast.Suspended)
                    {
                        // A `choose` stops the ability until a player answers,
                        // and resuming part-way through one is not written. So
                        // an effect *after* a choice is refused by name rather
                        // than run before the choice it was meant to follow --
                        // which is the reading that looks like it worked.
                        throw new RulesNotImplementedException(
                            $"'{cast.Source.FaceId}' has an effect after a choice, and "
                            + "resuming an ability part-way through is not implemented");
                    }

                    Run(step, cast);
                }

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

        "hasStatus" => Find(node.Require("card"), cast) is { } host
            && Statuses.Has(cast.World, host, Word(node.Require("status"))),
        _ => throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' uses the test node '{node.Kind}', "
            + "which is not implemented"),
    };

    private static void GiveStatus(AbilityNode node, Cast cast)
    {
        var host = Find(node.Require("card"), cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would give a status to a card that is not there");

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

        cast.World.Agenda.Then(new PhaseStep(
            Steps.ChooseOption,
            cast.World.Agenda.Current?.Round ?? 0,
            2,
            Index: cast.Player,
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

            // `result.*` -- what an action earlier in this ability actually
            // did, which is not what it was asked to do. Zero when nothing has
            // written it, so a card reading a result it never produced reads a
            // number rather than throwing: "no damage was healed" is exactly
            // the case where nothing ran.
            "result" => cast.Results.GetValueOrDefault(Word(node.Argument)),
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

        /// <summary>The card the player picked, once they have.</summary>
        public Card? Chosen { get; private set; }

        /// <summary>Records the card a <c>chooseCard</c> was answered with.</summary>
        /// <param name="card">What they picked.</param>
        public void Choose(Card card) => Chosen = card;
    }
}
