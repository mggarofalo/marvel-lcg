using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DiscardCostReceiptTests
{
    [Theory]
    [InlineData("01088", 2)]
    [InlineData("01055", 1)]
    public void EffectCountsOnlyTheCardDiscardedAsItsPhysicalCost(string discardedFace, int expected)
    {
        var runner = Runner("04011", "Action",
            """{"placeCounters":{"card":"you","counter":"observed","count":{"printedResourceIconsDiscarded":"game"}}}""",
            cost: """{"seq":[{"exhaust":"this"},{"spendAny":1},{"discardFromHand":1}]}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "04011");
        var payment = world.CreateCard("01089", world.Seats[0].Hand);
        var discarded = world.CreateCard(discardedFace, world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId], [discarded.ObjectId]));
        Assert.Equal(expected, world.Seats[0].IdentityCard.Tokens["c_observed"]);
        Assert.Equal(DeckType.DiscardPile, discarded.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.False(source.Ready);
    }
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EffectCountsPhysicalDiscardCostCardsSeparatelyFromResourcePayment(int count)
    {
        // Synthetic Shield Toss receipt shape: each physical hand discard is
        // one unit of X, regardless of its printed resource icons.
        var runner = Runner("04011", "Action",
            """{"placeCounters":{"card":"you","counter":"observed","count":{"cardsDiscarded":"game"}}}""",
            cost: $$"""{"seq":[{"exhaust":"this"},{"spendAny":1},{"discardFromHand":{{count}}}]}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "04011");
        var payment = world.CreateCard("01089", world.Seats[0].Hand);
        var discarded = Enumerable.Range(0, count)
            .Select(_ => world.CreateCard("01088", world.Seats[0].Hand)).ToArray();
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId],
            [.. discarded.Select(card => card.ObjectId)]));
        Assert.Equal(count, world.Seats[0].IdentityCard.Tokens["c_observed"]);
        Assert.All(discarded, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.False(source.Ready);
    }

}
