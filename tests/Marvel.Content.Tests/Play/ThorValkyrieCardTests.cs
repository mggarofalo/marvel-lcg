using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual full-book rows on nonpublic supported boards.
public sealed class ThorValkyrieCardTests : CoreCardTestBase
{
    [Theory]
    [InlineData("06022", null, 3)]
    [InlineData("06024", null, 2)]
    [InlineData("06016", "Y", 3)]
    [InlineData("06016", "R", 2)]
    public void ValkyrieEntryUsesItsActualPaidIcons(string resource, string? declared, int damage)
    {
        var runner = AuthoredCards.Runner();
        Card? ally = null;
        Card? minion = null;
        Card? first = null;
        Card? second = null;
        var (game, world) = Playing(board =>
        {
            var seat = board.Seats[0];
            minion = board.CreateCard("01130", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            ally = board.CreateCard("06012", seat.Hand);
            first = board.CreateCard(resource, seat.Hand);
            second = board.CreateCard("01003", seat.Hand);
        }, hero: true, abilities: runner);
        var allocations = new ResourceAllocation[]
        {
            new(first!.ObjectId, 0, declared is null ? (resource == "06022" ? "YY" : "RR") : declared + declared), new(second!.ObjectId, 0, "R"),
        };
        var play = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == ally!.ObjectId);
        game.Resolve(new Decision(play.Id, [world.Seats[0].IdentityCard.ObjectId], [first!.ObjectId, second!.ObjectId], Allocations: allocations));
        var response = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == ally!.ObjectId);
        game.Resolve(Decision.Take(response.Id));
        game.Resolve(Decision.Take(minion!.ObjectId));
        Assert.Equal(damage, minion.Damage);
        Assert.Equal(DeckType.AlliesArea, ally!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
        Assert.True(game.IsRootPrompt);
    }

    [Fact]
    public void PutIntoPlayReentryCannotReuseValkyriesEarlierEnergyPayment()
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[0];
        var ally = world.CreateCard("06012", seat.Hand);
        var energy = world.CreateCard("06022", seat.Hand);
        var single = world.CreateCard("01003", seat.Hand);
        var minion = world.CreateCard("01130", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        CardPlay.Play(world, world.Facts, runner, seat, ally, [energy.ObjectId, single.ObjectId], []);
        ResolveEntry(world, ally, minion);
        Assert.Equal(3, minion.Damage);
        // Direct discard prepares a new physical entry without paying its price.
        Discard.Card(world, ally, "fixture", []);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "fixture", []);
        ResolveEntry(world, ally, minion);
        Assert.Equal(5, minion.Damage);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
    }

    private static void ResolveEntry(World world, Card ally, Card target)
    {
        var runner = AuthoredCards.Runner();
        var response = Sequence.Work(world, world.Facts, runner, [])!;
        var offered = Assert.Single(response.Affordances, a => a.AnchorId == ally.ObjectId);
        Sequence.Answer(world, world.Facts, runner, response, Decision.Take(offered.Id), []);
        var choice = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Contains(choice.Affordances, a => a.AnchorId == target.ObjectId);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(target.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void ResponseChoosesAnyPlayersMinionRejectsVillainAndCanBeDeclined()
    {
        var world = ThorActualFixture.Board("06001b", 2);
        var runner = AuthoredCards.Runner();
        var ally = world.CreateCard("06012", world.Seats[0].Hand);
        var foreign = world.CreateCard("01130", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var own = world.CreateCard("01130", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "fixture", []);
        var response = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, response, Decision.Decline, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(0, foreign.Damage);
        Assert.Equal(0, own.Damage);
        Discard.Card(world, ally, "fixture", []);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "fixture", []);
        response = Sequence.Work(world, world.Facts, runner, [])!;
        var offered = Assert.Single(response.Affordances, a => a.AnchorId == ally.ObjectId);
        Sequence.Answer(world, world.Facts, runner, response, Decision.Take(offered.Id), []);
        var choice = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(new[] { own.ObjectId, foreign.ObjectId }.Order(), choice.Affordances.Select(a => a.AnchorId).Order());
        string before = world.Digest().Canonical();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner, choice,
            Decision.Take(world.TheCardIn(DeckType.VillainArea)!.ObjectId), []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(pending, world.Agenda.Outstanding);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(foreign.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(2, foreign.Damage);
        Assert.Equal(0, own.Damage);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void EntryWithoutAnyMinionOffersNoResponse()
    {
        var world = ThorActualFixture.Board("06001b");
        var runner = AuthoredCards.Runner();
        var ally = world.CreateCard("06012", world.Seats[0].Hand);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "fixture", []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
