using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class KangPlayerCountIndependentReviewTests : CoreCardTestBase
{
    [Fact]
    public void FourPlayerDepartureCanChooseANonadjacentAreaAndCarriesItsCards()
    {
        // pack:mc11:game-areas: choose an area; previous side schemes join it,
        // and engaged minions remain engaged with their player.
        var world = KangPlayerCountFixture.Board(4, false);
        var events = new List<GameEvent>();
        KangPlayerCountFixture.Defeat(world, Assert.Single(world.AreaOf(DeckType.VillainArea).Cards), events);
        KangPlayerCountFixture.Boundary(world, 1, events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        var order = world.PlayerOrder.ToArray();
        int departing = order[0];
        var side = world.CreateCard("01098", world.AreaOf(DeckType.SideSchemesArea, PlayArea.Of(departing)));
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(departing)));
        var villain = Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(departing)).Cards);
        KangPlayerCountFixture.Defeat(world, villain, events);
        KangPlayerCountFixture.Boundary(world, 2, events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        var destination = world.GameAreaOf(PlayArea.Of(order[2]))!;
        Assert.Contains(prompt.Affordances, offer => offer.Id == destination.Id);
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(destination.Id), events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Same(destination, world.GameAreaOf(PlayArea.Of(departing)));
        Assert.Same(destination, Places.GameAreaOf(world, side));
        Assert.Same(destination, Places.GameAreaOf(world, minion));
        Assert.Equal(PlayArea.Of(departing), minion.Area.PlayArea);
        Assert.NotSame(destination, world.GameAreaOf(PlayArea.Of(order[1])));
        Assert.NotSame(destination, world.GameAreaOf(PlayArea.Of(order[3])));
        Assert.Null(world.GameAreaOf(PlayArea.Villains));
    }
}
