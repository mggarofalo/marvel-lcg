using System.Text.Json;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic topology changes probe admission; these do not play or admit Kang.
public sealed class KangRetainedGroupBoundaryTests : CoreCardTestBase
{
    [Fact]
    public void ChangedUnchosenGroupCannotCommitSuspendedDestination()
    {
        var (world, first, destination, retained) = Prepare(false);
        var abilities = new NoCardAbilities();
        var events = new List<GameEvent>();
        var order = Assert.IsType<Prompt>(Sequence.Work(world, Cards, abilities, events));
        Sequence.Answer(world, Cards, abilities, order, Decision.Take(first.ObjectId), events);
        var join = Assert.IsType<Prompt>(Sequence.Work(world, Cards, abilities, events));
        world.Join(PlayArea.Of(3), retained, "uncontrolled topology change", events);
        string before = Snapshot(world, events);

        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, Cards, abilities,
            join, Decision.Take(destination.Id), events));
        Assert.Equal(before, Snapshot(world, events));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChangedUnchosenGroupCannotAdvancePreparedJoin(bool uniqueConflict)
    {
        var (world, first, destination, retained) = Prepare(uniqueConflict);
        var abilities = new NoCardAbilities();
        var events = new List<GameEvent>();
        var order = Assert.IsType<Prompt>(Sequence.Work(world, Cards, abilities, events));
        Sequence.Answer(world, Cards, abilities, order, Decision.Take(first.ObjectId), events);
        var join = Assert.IsType<Prompt>(Sequence.Work(world, Cards, abilities, events));
        Sequence.Answer(world, Cards, abilities, join, Decision.Take(destination.Id), events);
        world.Join(PlayArea.Of(3), retained, "uncontrolled topology change", events);
        string before = Snapshot(world, events);

        Assert.Throws<RulesNotImplementedException>(() => Sequence.Work(world, Cards, abilities, events));
        Assert.Equal(before, Snapshot(world, events));
    }

    private static (World World, Card First, GameArea Destination, GameArea Retained) Prepare(bool uniqueConflict)
    {
        var world = Hero("01001a,01001b", 4);
        foreach (var seat in world.Seats)
        {
            World.MoveToTop(seat.IdentityCard, seat.Hand);
            world.CreateCard("01089", seat.Deck);
        }
        var groups = Enumerable.Range(0, 3).Select(_ => world.CreateGameArea()).ToArray();
        for (int player = 0; player < groups.Length; player++)
            world.Join(PlayArea.Of(player), groups[player], "prepared split", []);
        world.Detach(PlayArea.Villains, "prepared split", []);
        string[] faces = ["11002", "11003", "11004"];
        var villains = faces.Select((face, player) =>
            world.CreateCard(face, world.AreaOf(DeckType.VillainArea, PlayArea.Of(player)))).ToArray();
        foreach (var villain in villains)
            world.Agenda.KangTransitions.CaptureJoin(world, villain, 7, PhaseTransitionKind.EndPlayerPhase);
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 7, 6));
        if (uniqueConflict)
        {
            world.CreateCard("01011", world.Seats[0].Hero);
            world.CreateCard("01011", world.Seats[1].Hero);
        }
        return (world, villains[0], groups[1], groups[2]);
    }

    private static string Snapshot(World world, List<GameEvent> events) => JsonSerializer.Serialize(new
    {
        State = KangPlayerCountFixture.Snapshot(world, events),
        Frames = world.Agenda.Outstanding.Select(step => new
        {
            Step = step,
            Occurrence = world.Agenda.Occurrence,
            Operation = JsonSerializer.Serialize(step.Operation, step.Operation.GetType()),
        }).ToArray(),
    });
}
