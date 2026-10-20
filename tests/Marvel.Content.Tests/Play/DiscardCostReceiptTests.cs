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
}
