using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>
/// Prelate Armor, and the first ability the engine has that costs something to
/// answer a window with.
/// </summary>
/// <remarks>
/// <para>
/// "Attach to Unus. [star] <b>Forced Response</b>: After Unus schemes, give him
/// a tough status card. <b>Hero Response</b>: After you make a basic attack
/// against Unus, spend [mental] [physical] resources → discard this card."
/// </para>
/// <para>
/// The second half is <c>01100</c> Enhanced Ivory Horn one tier over — the same
/// attachment on the villain, the same arrow cost, the same discard, offered in
/// a window rather than as an action. <c>rr:initiating-abilities.step.5</c> pays
/// before step 6 resolves and says nothing about which tier the ability sits
/// in, so a response with a cost is priced, paid and resolved exactly as an
/// action is.
/// </para>
/// <para>
/// <b>Two questions an action never had to ask.</b> Whose form to read, and
/// whose hand to price the cost against. An action is taken by a seat, so both
/// answers arrive with the request; a window is opened around an occurrence and
/// offered down the table. <c>rr:you-your.7</c> settles both — "you" is the
/// player the occurrence happened to — which is what the trigger's
/// <c>player</c> field says.
/// </para>
/// </remarks>
public sealed class PrelateArmorTests
{
    private const string Campaign = "unus";
    private const uint Seed = 12345;

    /// <summary>A card that generates one mental resource.</summary>
    private const string Mental = "01004";

    /// <summary>A card that generates one physical resource.</summary>
    private const string Physical = "01003";

    /// <summary>A card that generates one energy resource, which pays neither.</summary>
    private const string Energy = "01007";

    /// <summary>Spider-Man's ally, who can attack while his identity cannot.</summary>
    private const string BlackCat = "01002";

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:attach-to")]
    [Fact]
    public void TheArmorAttachesToUnusAsItEntersPlay()
    {
        // "Attach to Unus" is the phrase `rr:attach-to` is about, and the
        // scenario's villain is the only villain on the board -- so the card
        // names `villain` and the engine finds him, the same as Charge does.
        var board = Attacking();

        Assert.Equal(DeckType.UpgradesArea, board.Armor.Area.Type);
        Assert.Equal(board.Unus.ObjectId, board.Armor.Area.Host);
    }

    [Rule("rr:star-icon.2")]
    [Rule("rr:forced.1")]
    [Fact]
    public void AfterUnusSchemesHeIsGivenAToughStatusCard()
    {
        // The star reads as being about "the attached enemy", so "him" is
        // whatever the card hangs off rather than anything the scenario names.
        // Forced, so nobody is asked: `Finish` refuses to answer a question and
        // this raises none.
        var board = Attacking(hero: false);
        bool before = Statuses.Has(board.World, board.Unus, "tough");
        var events = Schemes(board);

        Assert.False(before);
        Assert.True(Statuses.Has(board.World, board.Unus, "tough"));

        // **"After" is the half of the sentence that says which window.** A
        // tough card is a tough card whichever side of the scheme it lands on,
        // so the board alone cannot tell a Forced Response from a Forced
        // Interrupt here -- the order of the two events can, and it is the only
        // thing that can. The threat goes on first.
        Assert.True(
            events.FindIndex(what => what.Verb == "Place_Threat")
                < events.FindIndex(what => what.Verb == "Give_Status"),
            "the threat is placed before the status is given");
    }

    [Rule("rr:scheme-enemy-activation.step.3")]
    [Fact]
    public void TheArmoursPrintedSchemeIconIsAddedToTheThreatUnusPlaces()
    {
        // "Place threat on the main scheme equal to the scheming enemy's
        // **modified** SCH value." The armour prints `SCH+ 1` and Unus's first
        // stage prints SCH 1, so an attachment nobody reads is the difference
        // between one threat and two.
        //
        // `rr:scheme-enemy-activation.step.3` says **modified** SCH, the same
        // word the attack's step 4 uses for ATK. Reading a printed number here
        // instead would make every modifier to a scheming enemy worth nothing.
        long armoured = Placed(armour: true);
        long bare = Placed(armour: false);

        Assert.Equal(1, bare);
        Assert.Equal(2, armoured);
    }

    [Rule("rr:scheme-enemy-activation.step.2")]
    [Fact]
    public void ABoostCardsOwnModifierIsInForceByTheTimeTheSchemeIsMeasured()
    {
        // Step 2 resolves the boost cards; step 3 reads the modified SCH. The
        // order is the rule and it is observable: a value read before the boost
        // card resolved would miss whatever the boost card did, which is the
        // other half of the same line.
        //
        // Hand-written, because no printed card in this engine's pool yet
        // changes SCH from a boost card — and the ordering is a property of the
        // step rather than of any card.
        var board = Bare();
        var main = board.World.TheCardIn(DeckType.MainSchemesArea)!;
        long before = main.Tokens.GetValueOrDefault("k_threat");

        var top = board.World.AreaOf(DeckType.EncounterDeck).Cards[^1];
        var abilities = new AbilityRunner(AbilityCatalog.Parse(
            $$"""
            { "cards": [ { "card": "{{top.FaceId}}", "abilities": [ {
                "trigger": { "event": "WhenCardRevealed", "timing": "Boost", "subject": "this" },
                "effect": { "grantUntil": { "card": { "query": "villain" },
                                            "keyword": "scheme", "amount": 2,
                                            "until": "EndOfActivation" } }
            } ] } ] }
            """));

        board.World.Abilities = abilities;
        Schemes(board with { Abilities = abilities });

        // Unus stage one prints SCH 1, and the boost card is worth two more.
        Assert.Equal(3, main.Tokens.GetValueOrDefault("k_threat") - before);
    }

    /// <summary>How much threat one scheme by Unus puts on the main scheme.</summary>
    private static long Placed(bool armour)
    {
        var board = armour ? Attacking(hero: false) : Bare();
        var main = board.World.TheCardIn(DeckType.MainSchemesArea)!;
        long before = main.Tokens.GetValueOrDefault("k_threat");
        Schemes(board);
        return main.Tokens.GetValueOrDefault("k_threat") - before;
    }

    /// <summary>The same board with the armour left in the encounter deck.</summary>
    private static Board Bare()
    {
        var abilities = AuthoredCards.Runner();
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, ["spider_man"]), Cards),
            ["Spider-Man"],
            Seed,
            abilities);

        var armor = world.Cards.First(card => card.FaceId == AuthoredCards.PrelateArmor);
        return new Board(world, abilities, armor, world.TheCardIn(DeckType.VillainArea)!);
    }

    [Rule("rr:initiating-abilities.step.3")]
    [Fact]
    public void AttackingUnusOffersTheDiscardAtItsPrintedPrice()
    {
        // Step 3 determines "the cost (or costs) to play the card or initiate
        // the ability" -- two resources, one of each named type. The menu of
        // ways to pay is the hand, which is why `Sources` is on the option
        // rather than a number being on the affordance.
        var board = Attacking();
        Hand(board, Mental, Physical);

        var asked = Attack(board);

        Assert.NotNull(asked);
        Assert.Equal(Question.Opportunity, asked.Asking);
        Assert.Equal(TimingPriority.Response, asked.When);
        Assert.Equal(0, asked.Player);

        var offer = Assert.Single(asked.Affordances);
        var price = Assert.Single(offer.Costs!);
        Assert.Equal("2", price.Cost);
        Assert.Equal(["BR"], price.Rule);
        Assert.Contains(price.Generators, source => source.Generates == "B");
        Assert.Contains(price.Generators, source => source.Generates == "R");
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:cost-arrow-icon")]
    [Fact]
    public void PayingDiscardsTheArmorAndTheCardsThatPaidForIt()
    {
        // "Non-bolded text before the cost arrow icon must be paid and/or
        // resolved in full before the text after the cost arrow icon can be
        // resolved." Both discards are the point: the two cards spent leave the
        // hand, and the armour leaves Unus.
        var board = Attacking();
        var (mental, physical) = Hand(board, Mental, Physical);

        var asked = Attack(board);
        var events = new List<GameEvent>();
        Sequence.Answer(
            board.World,
            Cards,
            board.Abilities,
            asked!,
            Decision.Take(board.Armor.ObjectId, [], [mental.ObjectId, physical.ObjectId]),
            events);

        Assert.Equal(DeckType.EncounterDiscardPile, board.Armor.Area.Type);
        Assert.Equal(DeckType.DiscardPile, mental.Area.Type);
        Assert.Equal(DeckType.DiscardPile, physical.Area.Type);
    }

    [Rule("rr:initiating-abilities.step.3")]
    [Fact]
    public void AHandThatCannotMeetTheTypesIsNotOfferedItAtAll()
    {
        // Step 3 checks "the player's ability to pay them", and only "if both
        // conditions are met" do the later steps happen -- so an unpayable
        // ability is not an offer that aborts at step 5. Two energy resources
        // are two resources and pay neither named type: `rr:resource.4` wants
        // "the specified types in the specified quantities".
        var board = Attacking();
        Hand(board, Energy, Energy);

        Assert.Null(Attack(board));
    }

    [Rule("rr:initiating-abilities.step.2")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnAlterEgoIsNotOfferedAHeroResponse(bool hero)
    {
        // "If the card or ability has a form requirement (for example, 'Hero
        // form only' or '**Hero** Action'), the form of the player playing that
        // card or initiating that ability is checked now." Step 2 is about any
        // ability and not only an action, and this is the first card in the
        // pool that prints a *Hero Response*.
        //
        // The attack itself is made by an ally, because an alter-ego cannot
        // make one: `rr:attack.1` is a hero-form basic power.
        var board = Attacking(hero);
        Hand(board, Mental, Physical);

        var cat = board.World.CreateCard(
            BlackCat,
            board.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));

        var events = new List<GameEvent>();
        BasicPowers.AllyPower(board.World, Cards, cat, board.Unus, BasicPowers.AttackVerb, events);
        var asked = Sequence.Work(board.World, Cards, board.Abilities, events);

        // The same attack, by the same ally, at the same price: the *only*
        // difference between the two runs is which way the identity card is
        // facing.
        Assert.Equal(hero, asked is not null);
    }

    [Rule("rr:you-your.7")]
    [Fact]
    public void AttackingSomethingOtherThanUnusOffersNothing()
    {
        // The trigger's target is `attachedTo`: "a basic attack against
        // **Unus**" is not every basic attack, and a minion on the same board
        // is the case that tells them apart.
        var board = Attacking();
        Hand(board, Mental, Physical);

        var soldier = board.World.CreateCard(
            AuthoredCards.InfiniteSoldier,
            board.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        var events = new List<GameEvent>();
        BasicPowers.BasicAttack(board.World, Cards, 0, soldier, events);

        Assert.Null(Sequence.Work(board.World, Cards, board.Abilities, events));
    }

    [Rule("rr:ability.11")]
    [Fact]
    public void DecliningLeavesTheArmourOnUnusAndNothingSpent()
    {
        // "Declining is the other answer", which is what makes an offer of one
        // ability a real choice -- and a cost that was described is not a cost
        // that was paid.
        var board = Attacking();
        var (mental, physical) = Hand(board, Mental, Physical);

        var asked = Attack(board);
        var events = new List<GameEvent>();
        Sequence.Answer(
            board.World, Cards, board.Abilities, asked!, Decision.Decline, events);

        Assert.Equal(DeckType.UpgradesArea, board.Armor.Area.Type);
        Assert.Equal(board.World.Seats[0].Hand, mental.Area);
        Assert.Equal(board.World.Seats[0].Hand, physical.Area);
    }

    [Rule("rr:forced.1")]
    [Fact]
    public void AMandatoryAbilityWithACostIsRefusedRatherThanPaidForByNobody()
    {
        // A forced ability "must be resolved when its triggering condition is
        // met", so `Offering.Work` runs it without asking anybody anything --
        // and a payment is an answer to a question. No card in the pool prints
        // one; the day one does, the window has to ask, and until then the
        // engine says so rather than resolving the effect for free.
        var board = Attacking();
        var runner = Runner(
            """
            "trigger": { "event": "WhenAttackInitiated", "timing": "ForcedResponse",
                         "target": "this" },
            "cost": { "spend": "BR" },
            "effect": { "discard": "this" }
            """);

        var refused = Assert.Throws<RulesNotImplementedException>(() => runner.Resolve(
            board.World,
            AttackOccurrence(board),
            new PendingAbility(board.Unus.ObjectId, AbilityType.ForcedResponse, 0),
            [],
            []));

        Assert.Contains("mandatory ability with a cost", refused.Message, StringComparison.Ordinal);
    }

    [Rule("rr:ability.8")]
    [Fact]
    public void AnAbilityOfferedToEveryPlayerHasNoHandToBePricedAgainst()
    {
        // "Any player may trigger an optional ability on an encounter card", so
        // an encounter card's ability belongs to no seat by default -- and a
        // cost belongs to one. The trigger's `player` is where a card that means
        // one seat says so; a card with a cost and without it is refused rather
        // than priced against the first player's hand.
        var board = Attacking();
        var runner = Runner(
            """
            "trigger": { "event": "WhenAttackInitiated", "timing": "Response",
                         "target": "this" },
            "cost": { "spend": "BR" },
            "effect": { "discard": "this" }
            """);

        var refused = Assert.Throws<RulesNotImplementedException>(() => runner.Waiting(
            board.World,
            AttackOccurrence(board),
            WindowKind.Response));

        Assert.Contains("offered to every player", refused.Message, StringComparison.Ordinal);
    }

    [Rule("rr:initiating-abilities.step.2")]
    [Fact]
    public void AnAbilityOfferedToEveryPlayerHasNoFormToRead()
    {
        // The same hole one field over: a form is a property of an identity,
        // and an ability offered down the table is not offered to an identity.
        var board = Attacking();
        var runner = Runner(
            """
            "trigger": { "event": "WhenAttackInitiated", "timing": "Response",
                         "target": "this", "form": "hero" },
            "effect": { "discard": "this" }
            """);

        var refused = Assert.Throws<RulesNotImplementedException>(() => runner.Waiting(
            board.World,
            AttackOccurrence(board),
            WindowKind.Response));

        Assert.Contains("no identity whose form to read", refused.Message, StringComparison.Ordinal);
    }

    [Rule("rr:you-your.7")]
    [Fact]
    public void ATriggerOfferedToSomebodyThisVocabularyDoesNotNameIsRefused()
    {
        // A closed set of one, for the reason `AbilitySubjects` is closed: "the
        // seat this happened to" is a relation between a card and an occurrence,
        // and naming the relations is what stops the field becoming a general
        // predicate.
        var refused = Assert.Throws<AbilityException>(() => Runner(
            """
            "trigger": { "event": "WhenAttackInitiated", "timing": "Response",
                         "target": "this", "player": "firstPlayer" },
            "effect": { "discard": "this" }
            """));

        Assert.Contains("'firstPlayer'", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>A runner holding one hand-written ability on Unus's own face.</summary>
    private static AbilityRunner Runner(string ability) =>
        new(AbilityCatalog.Parse(
            $$"""
            { "cards": [ { "card": "45059", "abilities": [ { {{ability}} } ] } ] }
            """));

    /// <summary>A player character attacking Unus.</summary>
    private static Occurrence AttackOccurrence(Board board) => Occurrence.ForAttack(
        0,
        [Steps.AttackInitiated],
        board.World,
        Cards,
        board.World.Seats[0].IdentityCard.ObjectId,
        board.Unus.ObjectId);

    /// <summary>The dealt Unus board, in hero form, with the armour on Unus.</summary>
    private static Board Attacking(bool hero = true)
    {
        var abilities = AuthoredCards.Runner();
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, Campaign, ["spider_man"]), Cards),
            ["Spider-Man"],
            Seed,
            abilities);

        if (hero)
        {
            world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        }

        // Into play by the card's own "Attach to Unus" rather than by being
        // placed here -- `rr:attach-to` makes the phrase a rule about a card
        // entering play, so the route into play is what resolves it.
        var armor = world.Cards.First(card => card.FaceId == AuthoredCards.PrelateArmor);
        Reveal.Resolve(world, Cards, armor, 0, []);

        return new Board(
            world, abilities, armor, world.TheCardIn(DeckType.VillainArea)!);
    }

    /// <summary>Puts exactly these cards in the hand, and nothing else.</summary>
    private static (Card First, Card Second) Hand(Board board, string first, string second)
    {
        var hand = board.World.Seats[0].Hand;
        foreach (var held in hand.Cards.ToList())
        {
            World.MoveToTop(held, board.World.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        }

        return (board.World.CreateCard(first, hand), board.World.CreateCard(second, hand));
    }

    /// <summary>A basic attack on Unus, walked as far as the engine can take it.</summary>
    private static Prompt? Attack(Board board)
    {
        var events = new List<GameEvent>();
        BasicPowers.BasicAttack(board.World, Cards, 0, board.Unus, events);
        return Sequence.Work(board.World, Cards, board.Abilities, events);
    }

    /// <summary>Unus scheming once, resolved as far as it goes.</summary>
    private static List<GameEvent> Schemes(Board board)
    {
        var events = new List<GameEvent>();
        board.World.Agenda.Add(new PhaseStep(
            Steps.Scheme, 1, 2, Index: 0, Subject: board.Unus.ObjectId, Seat: 0));
        Sequence.Finish(board.World, Cards, board.Abilities, events);
        return events;
    }

    private sealed record Board(
        World World, AbilityRunner Abilities, Card Armor, Card Unus);
}
