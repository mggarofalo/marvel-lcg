using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkLeapCardTests
{
    [Theory]
    [InlineData("01090", "R", 5)]
    [InlineData("01090", "G", 3)]
    [InlineData("01090", "B", 3)]
    [InlineData("01088", "R", 3)]
    [Rule("rr:wild-resource.1")]
    public void LeapUsesOnlyDeclaredResourcesActuallyPaid(string doubleResource, string wildAs, int removed)
    {
        // A generated wild may specify "which resource type ... it is being used as".
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        HulkFirstSliceFixture.EmptyHand(world, 0);
        var source = world.CreateCard("10004", world.Seats[0].Hand);
        var two = world.CreateCard(doubleResource, world.Seats[0].Hand);
        var wild = world.CreateCard("01011", world.Seats[0].Hand);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 10);
        long before = scheme.Tokens["k_threat"];
        var runner = AuthoredCards.Runner();
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        Assert.True(Assert.Single(runner.Describe(world, action).CostOptions).DeclarationSensitive);
        runner.ExecuteAction(world, new PlayerAction(action, [two.ObjectId, wild.ObjectId], [scheme.ObjectId],
            Allocations: [new(two.ObjectId, 0, doubleResource == "01090" ? "RR" : "YY"), new(wild.ObjectId, 0, wildAs)]));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(before - removed, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, two.Area.Type);
        Assert.Equal(DeckType.DiscardPile, wild.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("RR", 0, 5)]
    [InlineData("RG", 0, 3)]
    [InlineData("RB", 0, 3)]
    [InlineData("R", 1, 5)]
    [Rule("rr:wild-resource.1.1")]
    [Rule("rr:cost.4.1")]
    public void DoubledWildDeclarationsAndUnusedGeneratedIconsRemainDistinct(string paidAs, int reduction, int removed)
    {
        // Doubled wilds "can be declared a different type"; overpaid resources "were not paid for that cost".
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        HulkFirstSliceFixture.EmptyHand(world, 0);
        var source = world.CreateCard("10004", world.Seats[0].Hand);
        var doubleWild = world.CreateCard("01044", world.Seats[0].Hand);
        var physical = world.CreateCard("10005", world.Seats[0].Hand);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 10);
        long before = scheme.Tokens["k_threat"];
        if (reduction > 0) CardPayment.ReduceNextCardCost(world, world.Seats[0].IdentityCard, 0, reduction);
        var runner = AuthoredCards.Runner();
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [doubleWild.ObjectId, physical.ObjectId], [scheme.ObjectId],
            Allocations: [new(doubleWild.ObjectId, 0, paidAs), new(physical.ObjectId, 0, "R")]));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(before - removed, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, doubleWild.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void ZeroAdjustedPriceDoesNotQualifyAsPaymentUsingOnlyPhysicalResources()
    {
        // ruling:5bbba476a198a90c directly says zero-price Unstoppable Force:
        // "you did not pay for it using a physical resource ... do not draw 1 card".
        // Applying that interpretation to Leap's identical payment condition is an inference.
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        var source = world.CreateCard("10004", world.Seats[0].Hand);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 10);
        long before = scheme.Tokens["k_threat"];
        CardPayment.ReduceNextCardCost(world, world.Seats[0].IdentityCard, 0, 3);
        var runner = AuthoredCards.Runner();
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], [scheme.ObjectId]));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(before - 3, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }
}
