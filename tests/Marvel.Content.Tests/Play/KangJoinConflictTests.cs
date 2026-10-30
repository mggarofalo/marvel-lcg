using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Prepared nonpublic split areas; this does not execute Kang's phase controller.
public sealed class KangJoinConflictTests : CoreCardTestBase
{
    [Fact]
    public void WholeGroupJoinReportsEveryMatchingPhysicalPairAndExcludesOtherAreas()
    {
        // pack:mc11:rules-clarifications: "discard copies of unique cards until
        // only one of each remains in that game area."
        var world = Hero("01001a", 4);
        var source = world.CreateGameArea();
        var destination = world.CreateGameArea();
        var other = world.CreateGameArea();
        world.Join(PlayArea.Of(0), source, "prepared split", []);
        world.Join(PlayArea.Of(1), source, "prepared split", []);
        world.Join(PlayArea.Of(2), destination, "prepared split", []);
        world.Join(PlayArea.Of(3), other, "prepared split", []);
        world.Detach(PlayArea.Villains, "prepared split", []);
        // Remove synthetic duplicate identity fixtures from the comparison.
        foreach (var seat in world.Seats)
            World.MoveToTop(seat.IdentityCard, seat.Hand);
        var first = world.CreateCard("01011", world.Seats[0].Hero);
        var second = world.CreateCard("01011", world.Seats[1].Hero);
        var third = world.CreateCard("01011", world.Seats[2].Hero);
        world.CreateCard("01011", world.Seats[3].Hero);
        world.CreateCard("01011", world.Seats[0].Hand);
        world.CreateCard("01011", world.AreaOf(DeckType.SupportsArea));
        world.CreateCard("01041", world.Seats[2].Hero);

        var conflicts = Uniqueness.ConflictsAfterJoin(world, Cards, source, destination);

        Assert.Equal(new[] { (first, second), (first, third), (second, third) }, conflicts);
        Assert.Equal(conflicts, Uniqueness.ConflictsAfterJoin(world, Cards, destination, source));
    }

    [Fact]
    public void ConflictQueryPreservesCardsTopologyPendingWorkAndSeededStream()
    {
        var world = Hero("01001a", 2);
        var source = world.CreateGameArea();
        var destination = world.CreateGameArea();
        world.Join(PlayArea.Of(0), source, "prepared split", []);
        world.Join(PlayArea.Of(1), destination, "prepared split", []);
        world.CreateCard("01011", world.Seats[0].Hero);
        world.CreateCard("01011", world.Seats[1].Hero);
        world.CreateCard("01011", world.Seats[0].Hero);
        world.Agenda.Add(new PhaseStep(
            new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 1, 4));
        string before = world.Digest().Canonical();
        var pending = world.Agenda.Outstanding.ToArray();
        long words = world.Random.Generator.WordsConsumed;
        var random = world.Random.Generator.GetState();
        int areas = world.GameAreas.Count;

        Assert.NotEmpty(Uniqueness.ConflictsAfterJoin(world, Cards, source, destination));
        Assert.Empty(Uniqueness.ConflictsAfterJoin(world, Cards, source, source));

        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(areas, world.GameAreas.Count);
        Assert.Same(source, world.GameAreaOf(PlayArea.Of(0)));
        Assert.Same(destination, world.GameAreaOf(PlayArea.Of(1)));
    }

    [Fact]
    public void JoinQueryRejectsAnAreaFromAnotherWorld()
    {
        var world = Hero("01001a");
        var other = Hero("01001a");
        Assert.Throws<ArgumentException>(() => Uniqueness.ConflictsAfterJoin(
            world, Cards, world.GameAreas[0], other.GameAreas[0]));
    }
}
