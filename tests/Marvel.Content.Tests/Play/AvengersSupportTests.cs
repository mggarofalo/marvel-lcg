using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AvengersSupportTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("04021")]
    [InlineData("03024")]
    [Rule("rr:ally-limit")]
    public void TowerLimitRequiresEveryControlledAllyToBeAnAvenger(string face)
    {
        // Printed 04021: "If each of your allies has the Avenger trait".
        var world = Board();
        world.CreateCard(face, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Abilities = AvengersSupportCards.Runner();
        Assert.Equal(4, Limit(world, 0));
        Assert.Equal(3, Limit(world, 1));
        Ally(world, "01068", 0); // Vision: AVENGER.
        Ally(world, "01067", 1); // Another player's Maria Hill does not disable it.
        Assert.Equal(4, Limit(world, 0));
        var nonAvenger = Ally(world, "01067", 0);
        Assert.Equal(3, Limit(world, 0));
        World.MoveToTop(nonAvenger, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        Assert.Equal(4, Limit(world, 0));
    }

    [Theory]
    [InlineData("04021")]
    [InlineData("03024")]
    [Rule("rr:lasting-effects.5")]
    public void TowerReductionMatchesTraitAndTypeAcrossPlayersWithoutBeingSpentByPricing(string face)
    {
        // Printed 04021: "the next Avenger ally played this phase", without a player restriction.
        var world = Board();
        var tower = world.CreateCard(face, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var runner = AvengersSupportCards.Runner();
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == tower.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.False(tower.Ready);
        var ally = world.CreateCard("01068", world.Seats[1].Hand);
        var other = world.CreateCard("01067", world.Seats[1].Hand);
        var avengerSupport = world.CreateCard(face, world.Seats[1].Hand);
        Assert.Equal(3, CardPayment.CostOf(world, Cards, world.Seats[1], ally).Amount);
        Assert.Equal(2, CardPayment.CostOf(world, Cards, world.Seats[1], other).Amount);
        Assert.Equal(2, CardPayment.CostOf(world, Cards, world.Seats[1], avengerSupport).Amount);
        CardPayment.UseCostModifiers(world, CardPayment.CostOf(world, Cards, world.Seats[1], other));
        Assert.Equal(3, CardPayment.CostOf(world, Cards, world.Seats[1], ally).Amount);
        CardPayment.UseCostModifiers(world, CardPayment.CostOf(world, Cards, world.Seats[1], ally));
        Assert.Equal(4, CardPayment.CostOf(world, Cards, world.Seats[1], ally).Amount);
    }

    [Theory]
    [InlineData("04021")]
    [InlineData("03024")]
    [Rule("rr:lasting-effects.5")]
    public void UnusedTowerReductionExpiresAtPhaseEnd(string face)
    {
        var world = Board();
        var tower = world.CreateCard(face, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var runner = AvengersSupportCards.Runner();
        world.Abilities = runner;
        runner.ExecuteAction(world, new PlayerAction(Assert.Single(runner.Actions(world, 0)), [], []));
        var ally = world.CreateCard("01068", world.Seats[0].Hand);
        Assert.Equal(3, CardPayment.CostOf(world, Cards, world.Seats[0], ally).Amount);
        world.Effects.Expire(TimingPoints.EndOfPhase);
        Assert.Equal(4, CardPayment.CostOf(world, Cards, world.Seats[0], ally).Amount);
        Assert.False(tower.Ready);
    }

    [Fact]
    [Rule("rr:cost.7")]
    [Rule("rr:exhausted.2")]
    public void EarthsMightiestHeroesExhaustsOnlyAnAvengerAndReadiesAnotherControlledAvenger()
    {
        Card? source = null;
        Card? paid = null;
        Card? target = null;
        Card? wrongTrait = null;
        Card? wrongPlayer = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("04022", board.Seats[0].Hand);
            paid = Ally(board, "01068", 0);
            target = Ally(board, "01050", 0); // Hulk: AVENGER.
            target.Exhaust();
            wrongTrait = Ally(board, "01067", 0);
            wrongPlayer = Ally(board, "01068", 1);
            wrongPlayer.Exhaust();
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: AvengersSupportCards.Runner());
        var offer = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId);
        Assert.Contains("AVENGER", offer.CostDescription);
        Assert.Contains(paid!.ObjectId, offer.Targets!.Legal);
        Assert.DoesNotContain(wrongTrait!.ObjectId, offer.Targets.Legal);
        Assert.DoesNotContain(wrongPlayer!.ObjectId, offer.Targets.Legal);
        var before = world.Digest().Canonical();
        Assert.ThrowsAny<ArgumentException>(() => game.Resolve(Decision.Take(offer.Id, [wrongTrait.ObjectId], [])));
        Assert.Equal(before, world.Digest().Canonical());
        game.Resolve(Decision.Take(offer.Id, [paid.ObjectId], []));
        Assert.False(paid.Ready);
        Assert.Contains("AVENGER", game.Pending!.DisplayQuestion);
        var ready = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == target!.ObjectId);
        Assert.DoesNotContain(game.Pending.Affordances, item => item.AnchorId == paid.ObjectId);
        Assert.DoesNotContain(game.Pending.Affordances, item => item.AnchorId == wrongPlayer.ObjectId);
        game.Resolve(Decision.Take(ready.Id));
        Assert.True(target!.Ready);
        Assert.False(paid.Ready);
        Assert.False(wrongPlayer.Ready);
        Assert.Same(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0), source!.Area);
    }

    [Theory]
    [InlineData("only-self")]
    [InlineData("wrong-target-trait")]
    [InlineData("other-controller")]
    [InlineData("exhausted-cost")]
    [InlineData("alter-ego")]
    [Rule("rr:cost.7")]
    [Rule("rr:exhausted.2")]
    public void EarthsMightiestHeroesDoesNotOfferWhenItsPrintedExchangeCannotBeMade(string boundary)
    {
        var world = Board();
        var source = world.CreateCard("04022", world.Seats[0].Hand);
        if (boundary != "only-self")
        {
            if (boundary != "wrong-target-trait") world.Seats[0].IdentityCard.Exhaust();
            var paid = Ally(world, "01068", boundary == "other-controller" ? 1 : 0);
            if (boundary == "exhausted-cost") paid.Exhaust();
            var target = Ally(world, boundary == "wrong-target-trait" ? "01067" : "01050", 0);
            target.Exhaust();

            if (boundary == "alter-ego") world.Seats[0].IdentityCard.TurnTo(AuthoredCards.PeterParker);
        }
        var runner = AvengersSupportCards.Runner();
        world.Abilities = runner;
        var before = world.Digest().Canonical();
        Assert.DoesNotContain(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
    }

    private static long Limit(World world, int player) => StateFields.Modified(
        world, world.Seats[player].IdentityCard, "ally_limit", Cards, world.Players);
    private static Card Ally(World world, string face, int player) => world.CreateCard(face,
        world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player));
    private static World Board()
    {
        var world = new World(Cards, players: 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
        }
        return world;
    }
}
