using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic action isolates the mandatory printed-icon discard choice.</summary>
public sealed class ResourceIconDiscardTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void PrintedIconsOfferMinimalGroupsWithoutResourceGenerators(int count)
    {
        var runner = Runner("01007", "Action", $$"""{"discardResourceIcons":{{count}}}""");
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, "01007"), abilities: runner);
        Hand(world, "01003", 0);
        var first = world.CreateCard("01003", world.Seats[0].Hand);
        var second = world.CreateCard("01004", world.Seats[0].Hand);
        var doubleResource = world.CreateCard("01088", world.Seats[0].Hand);
        var generator = InPlay(world, "01008");
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId).Id));
        var option = Assert.Single(game.Pending!.Affordances);
        var request = Assert.IsType<TargetRequest>(option.Targets);
        Assert.DoesNotContain(generator.ObjectId, request.Legal);
        Assert.True(request.Allows([doubleResource.ObjectId]));
        Assert.Equal(count == 1, request.Allows([first.ObjectId]));
        Assert.Equal(count == 2, request.Allows([first.ObjectId, second.ObjectId]));
        Assert.False(request.Allows([first.ObjectId, doubleResource.ObjectId]));
        Assert.Contains("printed", game.Pending.Description);
        string before = world.Digest().Canonical();
        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(option.Id, [first.ObjectId, doubleResource.ObjectId], [])));
        Assert.Equal(before, world.Digest().Canonical());
        game.Resolve(new Decision(option.Id, [doubleResource.ObjectId], []));
        Assert.Equal(DeckType.DiscardPile, doubleResource.Area.Type);
        Assert.Same(world.Seats[0].Hand, first.Area);
        Assert.True(generator.Ready);
    }

    [Fact]
    public void InsufficientIconsStillDiscardWhatIsAvailable()
    {
        var runner = Runner("01007", "Action", """{"discardResourceIcons":2}""");
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, "01007"), abilities: runner);
        Hand(world, "01003", 1);
        var only = Assert.Single(world.Seats[0].Hand.Cards);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId).Id));
        var option = Assert.Single(game.Pending!.Affordances);
        Assert.True(option.Targets!.Allows([only.ObjectId]));
        game.Resolve(new Decision(option.Id, [only.ObjectId], []));
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Equal(DeckType.DiscardPile, only.Area.Type);
    }

    [Fact]
    public void EmptyHandCompletesWithoutAnEmptyChoice()
    {
        var runner = Runner("01007", "Action", """{"seq":[{"discardResourceIcons":2},{"placeCounters":{"card":"this","counter":"finished","count":1}}]}""");
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, "01007"), abilities: runner);
        Hand(world, "01003", 0);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId).Id));
        Assert.Equal(1, source!.Tokens["c_finished"]);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
    }
}
