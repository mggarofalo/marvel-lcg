using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows on synthetic boards.
public sealed class QuicksilverSignatureTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:ready.1")]
    public void AlwaysBeRunningReadiesQuicksilverOnlyWhenHeIsExhausted()
    {
        // rr:ready.1: "If a player is instructed to ready an exhausted card, the card
        // is returned to its ready state." 14003: "Hero Action: Ready Quicksilver."
        var world = QuicksilverBoards.Board();
        var quicksilver = world.Seats[0].IdentityCard;
        var running = QuicksilverBoards.Hand(world, "14003");
        var energy = QuicksilverBoards.Hand(world, "01088");
        Assert.DoesNotContain(QuicksilverBoards.Runner(world).Actions(world, 0), a => a.Card == running.ObjectId);

        quicksilver.Exhaust();
        QuicksilverBoards.Play(world, running, [energy]);
        QuicksilverBoards.Drive(world, _ => null);

        Assert.True(quicksilver.Ready);
        Assert.Equal(DeckType.DiscardPile, running.Area.Type);
    }

    [Fact]
    [Rule("rr:max-maximum")]
    [Rule("rr:lasting-effects")]
    public void MaximumVelocityBoostsEveryPowerUntilTheRoundEndsAndIsMaxOnePerPhase()
    {
        // rr:max-maximum: a maximum counts every copy by title. rr:lasting-effects: the
        // modifiers last until the end of the round. 14005: "Max 1 per phase. Hero
        // Action: You get +2 THW, +2 ATK, and +2 DEF until the end of the round."
        var world = QuicksilverBoards.Board();
        var quicksilver = world.Seats[0].IdentityCard;
        var first = QuicksilverBoards.Hand(world, "14005");
        var second = QuicksilverBoards.Hand(world, "14005");
        var paying = new[] { QuicksilverBoards.Hand(world, "01090") };

        QuicksilverBoards.Play(world, first, paying);
        QuicksilverBoards.Drive(world, _ => null);

        Assert.Equal(3, Modified(world, quicksilver, "thwart"));
        Assert.Equal(3, Modified(world, quicksilver, "attack"));
        Assert.Equal(3, Modified(world, quicksilver, "defense"));
        Assert.DoesNotContain(QuicksilverBoards.Runner(world).Actions(world, 0), a => a.Card == second.ObjectId);
    }

    [Theory]
    [InlineData("14008", "defense")]
    [InlineData("14010", "thwart")]
    [InlineData("14011", "attack")]
    [Rule("rr:upgrade.1")]
    public void EachPowerUpgradeGivesQuicksilverOne(string upgrade, string field)
    {
        // rr:upgrade.1: "An upgrade is active so long as it is in play".
        var world = QuicksilverBoards.Board();
        var quicksilver = world.Seats[0].IdentityCard;
        long before = Modified(world, quicksilver, field);
        QuicksilverBoards.InPlay(world, upgrade, DeckType.UpgradesArea);
        Assert.Equal(before + 1, Modified(world, quicksilver, field));
    }

    [Fact]
    [Rule("rr:choose-option.2")]
    public void DoubleTimeMayChooseTheSameOptionTwice()
    {
        // 14004: "Choose two of the following (you may choose the same option twice)".
        // rr:choose-option.2: a player-card option must at least partially resolve.
        // Ruling 5a46f4ebeb92ff9d: options are chosen before resolving and each needs
        // its own valid target.
        var world = QuicksilverBoards.Board();
        var villain = QuicksilverBoards.Villain(world);
        var minion = QuicksilverBoards.Engaged(world, "01101");
        var main = QuicksilverBoards.MainScheme(world);
        var time = QuicksilverBoards.Hand(world, "14004");
        QuicksilverBoards.Play(world, time, [QuicksilverBoards.Hand(world, "01089")]);

        var choice = Assert.IsType<Prompt>(Sequence.Work(world, Cards, QuicksilverBoards.Runner(world), []));
        // No scheme holds threat, so only twice damage is offered.
        var offer = Assert.Single(choice.Affordances);
        Assert.Contains("Deal 2 damage to an enemy, then deal 2 damage", offer.Description, StringComparison.Ordinal);
        Sequence.Answer(world, Cards, QuicksilverBoards.Runner(world), choice, Decision.Take(offer.Id), []);
        var targets = new Queue<Card>([villain, minion]);
        QuicksilverBoards.Drive(world, prompt => targets.Count > 0
            && prompt.Affordances.FirstOrDefault(a => a.Id == targets.Peek().ObjectId) is not null
                ? Decision.Take(targets.Dequeue().ObjectId) : null);

        Assert.Equal(2, villain.Damage);
        Assert.Equal(2, minion.Damage);
        Assert.Equal(0, main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:crisis-icon.1")]
    public void DoubleTimeRemovesThreatFromTheMainSchemeOnceTheCrisisSchemeLeaves()
    {
        // rr:crisis-icon.1: a crisis icon is "equivalent to the following constant
        // ability: player cards cannot remove threat from the main scheme." Ruling
        // 5a46f4ebeb92ff9d: Double Time can clear a crisis side scheme with its first
        // sentence and then remove threat from the main scheme with its second.
        var world = QuicksilverBoards.Board();
        var main = QuicksilverBoards.MainScheme(world);
        main.PlaceTokens("k_threat", 5);
        var crisis = QuicksilverBoards.Side(world, "01108", 2);
        var time = QuicksilverBoards.Hand(world, "14004");
        QuicksilverBoards.Play(world, time, [QuicksilverBoards.Hand(world, "01089")]);

        QuicksilverBoards.Drive(world, prompt =>
            prompt.Affordances.FirstOrDefault(a => a.Description?.StartsWith("Remove 2 threat from a scheme, then remove", StringComparison.Ordinal) == true) is { } both
                ? Decision.Take(both.Id)
            : prompt.Affordances.FirstOrDefault(a => a.Id == crisis.ObjectId) is { } c ? Decision.Take(c.Id)
            : prompt.Affordances.FirstOrDefault(a => a.Id == main.ObjectId) is { } m ? Decision.Take(m.Id) : null);

        Assert.Equal(DeckType.EncounterDiscardPile, crisis.Area.Type);
        Assert.Equal(3, main.Tokens["k_threat"]);
    }

    [Fact]
    [Rule("rr:identity-specific-card")]
    [Rule("rr:choose-game-element.3")]
    public void ServalIndustriesShufflesTwoQuicksilverCardsFromTheDiscardPile()
    {
        // rr:identity-specific-card: "cards that belong to an identity's set of
        // accompanying cards." rr:choose-game-element.3: "simultaneously choose as many
        // as are available, to a maximum of the specified number." Two copies of
        // Always Be Running qualify together; the Energy does not.
        var world = QuicksilverBoards.Board("14001b");
        var serval = QuicksilverBoards.InPlay(world, "14007", DeckType.SupportsArea);
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        var running = new[] { world.CreateCard("14003", discard), world.CreateCard("14003", discard) };
        var reflex = world.CreateCard("14008", discard);
        var energy = world.CreateCard("01088", discard);
        int deck = world.Seats[0].Deck.Cards.Count;

        QuicksilverBoards.Play(world, serval, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, QuicksilverBoards.Runner(world), []));
        var choose = Assert.Single(prompt.Affordances);
        Assert.Equal([running[0].ObjectId, running[1].ObjectId, reflex.ObjectId], choose.Targets!.Legal.Order());
        Assert.Equal(2, choose.Targets.Min);
        Assert.Equal(2, choose.Targets.Max);
        Sequence.Answer(world, Cards, QuicksilverBoards.Runner(world), prompt,
            Decision.Take(choose.Id) with { Targets = [running[0].ObjectId, running[1].ObjectId] }, []);
        QuicksilverBoards.Drive(world, _ => null);

        Assert.All(running, card => Assert.Equal(DeckType.PlayerDeck, card.Area.Type));
        Assert.Equal(DeckType.DiscardPile, reflex.Area.Type);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Equal(deck + 2, world.Seats[0].Deck.Cards.Count);
        Assert.False(serval.Ready);
    }

    [Fact]
    [Rule("rr:choose-game-element.3")]
    public void ServalIndustriesShufflesTheOneQuicksilverCardThereIs()
    {
        // rr:choose-game-element.3: with one eligible card, that one is chosen.
        var world = QuicksilverBoards.Board("14001b");
        var serval = QuicksilverBoards.InPlay(world, "14007", DeckType.SupportsArea);
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        var reflex = world.CreateCard("14008", discard);
        world.CreateCard("01088", discard);

        QuicksilverBoards.Play(world, serval, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, QuicksilverBoards.Runner(world), []));
        var choose = Assert.Single(prompt.Affordances);
        Assert.Equal(1, choose.Targets!.Min);
        Assert.Equal(1, choose.Targets.Max);
        Sequence.Answer(world, Cards, QuicksilverBoards.Runner(world), prompt,
            Decision.Take(choose.Id) with { Targets = [reflex.ObjectId] }, []);
        QuicksilverBoards.Drive(world, _ => null);
        Assert.Equal(DeckType.PlayerDeck, reflex.Area.Type);
    }

    [Fact]
    [Rule("rr:ability.8")]
    public void ServalIndustriesIsAnAlterEgoActionThatNeedsAQuicksilverCard()
    {
        // rr:ability.8: an Action is optional, so it is offered only when it can
        // change the game; 14007 is an Alter-Ego Action.
        var world = QuicksilverBoards.Board("14001b");
        var serval = QuicksilverBoards.InPlay(world, "14007", DeckType.SupportsArea);
        world.CreateCard("01088", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        Assert.DoesNotContain(QuicksilverBoards.Runner(world).Actions(world, 0), a => a.Card == serval.ObjectId);

        world.CreateCard("14003", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        Assert.Contains(QuicksilverBoards.Runner(world).Actions(world, 0), a => a.Card == serval.ObjectId);
        world.Seats[0].IdentityCard.TurnTo("14001a");
        Assert.DoesNotContain(QuicksilverBoards.Runner(world).Actions(world, 0), a => a.Card == serval.ObjectId);
    }

    [Fact]
    [Rule("rr:response.1")]
    [Rule("rr:ready.1")]
    public void FrictionResistanceReadiesAfterSuperSpeedReadiesQuicksilver()
    {
        // 14009: "Hero Response: After you ready Quicksilver, ready this card."
        // rr:ready.1: only an exhausted card is returned to its ready state, so the
        // response waits for an exhausted Friction Resistance.
        var world = QuicksilverBoards.Board();
        var quicksilver = world.Seats[0].IdentityCard;
        var friction = QuicksilverBoards.InPlay(world, "14009", DeckType.UpgradesArea);
        friction.Exhaust();
        var main = QuicksilverBoards.MainScheme(world);
        main.PlaceTokens("k_threat", 3);
        var offered = new List<int>();

        BasicThwartPowers.BasicThwart(world, Cards, 0, main, []);
        QuicksilverBoards.Drive(world, prompt =>
            QuicksilverBoards.Take(quicksilver)(prompt) ?? QuicksilverBoards.Take(friction, offered)(prompt));

        Assert.True(quicksilver.Ready);
        Assert.Single(offered);
        Assert.True(friction.Ready);
    }

    [Fact]
    [Rule("rr:ready.1")]
    public void FrictionResistanceIgnoresAReadyThatLeavesQuicksilverAlone()
    {
        // Another player's hero readying is not "you ready Quicksilver".
        var world = QuicksilverBoards.Board(players: 2);
        var friction = QuicksilverBoards.InPlay(world, "14009", DeckType.UpgradesArea);
        friction.Exhaust();
        var spider = world.Seats[1].IdentityCard;
        spider.Exhaust();
        var offered = new List<int>();

        Assert.False(CardReadying.Ready(world, new CardReadyBatch([spider], null, "test"), []));
        QuicksilverBoards.Drive(world, QuicksilverBoards.Take(friction, offered));

        Assert.True(spider.Ready);
        Assert.Empty(offered);
        Assert.False(friction.Ready);
    }

    [Fact]
    [Rule("rr:resource-ability.1")]
    public void FrictionResistanceExhaustsForAPhysicalResource()
    {
        // 14009: "Resource: Exhaust Friction Resistance -> generate a [physical] resource."
        var world = QuicksilverBoards.Board();
        var friction = QuicksilverBoards.InPlay(world, "14009", DeckType.UpgradesArea);
        var card = QuicksilverBoards.Hand(world, "14003");
        var source = Assert.Single(CardPayment.Generators(world, world.Facts, world.Seats[0], QuicksilverBoards.Runner(world), card),
            generator => generator.Effect == friction.ObjectId);
        Assert.Equal("R", source.Generates);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [Rule("rr:non-numerical-variable.1")]
    [Rule("rr:choose-game-element.3")]
    public void SpeedCycloneStunsXEnemiesForXResourcesOfAnyType(int x, int stunned)
    {
        // 14006 costs X: "Hero Action: Stun X Enemies." rr:non-numerical-variable.1: "the
        // value of X is defined by ... player choice". rr:choose-game-element.3: with
        // fewer enemies than X, every available enemy is chosen. Rhino and one minion.
        var world = QuicksilverBoards.Board();
        var villain = QuicksilverBoards.Villain(world);
        var minion = QuicksilverBoards.Engaged(world, "01101");
        var cyclone = QuicksilverBoards.Hand(world, "14006");
        // One mental, one physical and one energy icon: X takes resources of any type.
        string[] single = ["14004", "14005", "14003"];
        Card[] paying = [.. Enumerable.Range(0, x).Select(index => QuicksilverBoards.Hand(world, single[index]))];
        var runner = QuicksilverBoards.Runner(world);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == cyclone.ObjectId);

        var price = AbilityPaymentPricing.CombinedPrice(world, cyclone, 0,
            Assert.Single(AuthoredCards.Program.On("14006")), runner)!;
        Assert.Equal(x, Assert.Single(price.VariableRequests).Max);
        var values = new Dictionary<string, long>(StringComparer.Ordinal) { ["X"] = x };
        int[] spent = [.. paying.Select(card => card.ObjectId)];

        runner.ExecuteAction(world, new PlayerAction(action, spent, [], values)
            { Allocations = [.. ResourcePayment.Allocate(price, spent, values)!] });
        QuicksilverBoards.Drive(world, prompt => prompt.Affordances.FirstOrDefault(a => a.Targets is { } request && request.Min > 0) is { } choose
            ? Decision.Take(choose.Id) with { Targets = [.. choose.Targets!.Legal.Take(choose.Targets.Min)] } : null);

        Assert.Equal(stunned, new[] { villain, minion }.Count(enemy => Statuses.Has(world, enemy, Statuses.Stunned)));
        Assert.All(paying, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.Equal(DeckType.DiscardPile, cyclone.Area.Type);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(1, 0)]
    [Rule("rr:non-numerical-variable.1")]
    public void AHelicarrierReductionLowersWhatSpeedCyclonePaysButNotX(int x, int paid)
    {
        // rr:non-numerical-variable.1: "the value of X is defined by card ability or
        // player choice, after which the amount paid may be modified by effects without
        // changing the value of X." 01092 Helicarrier: "choose a player. Reduce the
        // resource cost of the next card that player plays this phase by 1." Spider-Man
        // uses it on Quicksilver, who then stuns X enemies while paying X - 1.
        var world = QuicksilverBoards.Board(players: 2);
        var villain = QuicksilverBoards.Villain(world);
        var minion = QuicksilverBoards.Engaged(world, "01101");
        var helicarrier = QuicksilverBoards.InPlay(world, "01092", DeckType.SupportsArea, player: 1);
        QuicksilverBoards.Play(world, helicarrier, []);
        QuicksilverBoards.Drive(world, QuicksilverBoards.Take(world.Seats[0].IdentityCard));
        var cyclone = QuicksilverBoards.Hand(world, "14006");
        var later = QuicksilverBoards.Hand(world, "14003");
        Card[] paying = [.. Enumerable.Range(0, paid).Select(_ => QuicksilverBoards.Hand(world, "14005"))];
        var runner = QuicksilverBoards.Runner(world);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == cyclone.ObjectId);

        var price = AbilityPaymentPricing.CombinedPrice(world, cyclone, 0,
            Assert.Single(AuthoredCards.Program.On("14006")), runner)!;
        Assert.Equal("X-1", price.ResourceCosts[^1].Cost);
        var values = new Dictionary<string, long>(StringComparer.Ordinal) { ["X"] = x };
        int[] spent = [.. paying.Select(card => card.ObjectId)];

        runner.ExecuteAction(world, new PlayerAction(action, spent, [], values)
            { Allocations = [.. ResourcePayment.Allocate(price, spent, values)!] });
        QuicksilverBoards.Drive(world, prompt => prompt.Affordances.FirstOrDefault(a => a.Targets is { } request && request.Min > 0) is { } choose
            ? Decision.Take(choose.Id) with { Targets = [.. choose.Targets!.Legal.Take(choose.Targets.Min)] } : null);

        Assert.Equal(x, new[] { villain, minion }.Count(enemy => Statuses.Has(world, enemy, Statuses.Stunned)));
        Assert.All(paying, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.Equal(DeckType.DiscardPile, cyclone.Area.Type);
        // The reduction applied to Speed Cyclone; the next card costs its printed 1.
        Assert.Equal(1, CardPayment.CostOf(world, world.Facts, world.Seats[0], later).Amount);
    }
}
