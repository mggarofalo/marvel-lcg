using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PlayAreaSelectorTests : CoreCardTestBase
{
    [Fact]
    public void PrintedProfileSearchIncludesOnlyTheResolversInPlayCopy()
    {
        // Synthetic action isolates Marked for Death's printed search boundary.
        var runner = Runner("01006", "Action", """
            {"removeFromGame":{"cardsIn":{"area":"yourPlayArea",
              "kind":"Ally","title":"Mockingbird","cardClass":"Hero"}}}
            """);
        var world = Hero("04001a", 2);
        world.Abilities = runner;
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var ours = world.CreateCard("04004", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var theirs = world.CreateCard("04004", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var basic = world.CreateCard("01083", ours.Area);
        var hidden = world.CreateCard("04004", world.Seats[0].Deck);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);

        runner.ExecuteAction(world, new PlayerAction(action, [], []));

        Assert.Equal(DeckType.RemovedArea, ours.Area.Type);
        Assert.Equal(DeckType.AlliesArea, theirs.Area.Type);
        Assert.Equal(DeckType.AlliesArea, basic.Area.Type);
        Assert.Same(world.Seats[0].Deck, hidden.Area);
        Assert.False(hidden.FaceUp);
    }
}
