using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Returning a source pays a physical cost before its post-arrow effect.</summary>
public sealed class ReturnToHandCostTests
{
    [Theory]
    [InlineData("01088")]
    [InlineData("01089")]
    [InlineData("01090")]
    [InlineData("01055")]
    public void PaysResourceAndReturnsExactSourceWhileDiscardingItsAttachments(string resourceFace)
    {
        var runner = Runner("04004", "Action", """{"draw":{"player":"you","count":1}}""",
            cost: """{"seq":[{"spendAny":1},{"returnToHand":"this"}]}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "04004");
        var attached = world.CreateCard("01069", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(0), source.ObjectId, 0));
        var resource = world.CreateCard(resourceFace, world.Seats[0].Hand);
        var drawn = world.CreateCard("01093", world.Seats[0].Deck);
        string before = world.Digest().Canonical();
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(action, [], [])));
        Assert.Equal(before, world.Digest().Canonical());
        runner.ExecuteAction(world, new PlayerAction(action, [resource.ObjectId], []));
        Assert.Same(world.Seats[0].Hand, source.Area);
        Assert.Same(world.Seats[0].Hand, drawn.Area);
        Assert.Equal(DeckType.DiscardPile, attached.Area.Type);
        Assert.Equal(DeckType.DiscardPile, resource.Area.Type);
        Assert.DoesNotContain(runner.Actions(world, 0), option => option.Card == source.ObjectId);
    }

    [Fact]
    public void StaleSourceDepartureCannotPayOrResolveTheEffect()
    {
        var runner = Runner("04004", "Action", """{"draw":{"player":"you","count":1}}""",
            cost: """{"returnToHand":"this"}""");
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "04004");
        var action = Assert.Single(runner.Actions(world, 0), option => option.Card == source.ObjectId);
        Discard.Card(world, source, "test", []);
        string before = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world, new PlayerAction(action, [], [])));
        Assert.Equal(before, world.Digest().Canonical());
    }
}
