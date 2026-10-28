using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class KangRetainedCaptureReviewTests : CoreCardTestBase
{
    [Fact]
    public void ChangedRemainingGroupCannotCommitSuspendedUniqueDiscard()
    {
        var world = Hero("01001a,01001b", 3);
        foreach (var seat in world.Seats)
        {
            World.MoveToTop(seat.IdentityCard, seat.Hand);
            world.CreateCard("01089", seat.Deck);
        }
        var source = world.CreateGameArea();
        var destination = world.CreateGameArea();
        world.Join(PlayArea.Of(0), source, "prepared split", []);
        world.Join(PlayArea.Of(1), destination, "prepared split", []);
        world.Detach(PlayArea.Villains, "prepared split", []);
        var first = world.CreateCard("11002", world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)));
        var second = world.CreateCard("11003", world.AreaOf(DeckType.VillainArea, PlayArea.Of(1)));
        var unique = world.CreateCard("01011", world.Seats[0].Hero);
        world.CreateCard("01011", world.Seats[1].Hero);
        world.Agenda.KangTransitions.CaptureJoin(world, first, 7, PhaseTransitionKind.EndPlayerPhase);
        world.Agenda.KangTransitions.CaptureJoin(world, second, 7, PhaseTransitionKind.EndPlayerPhase);
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 7, 6));
        var abilities = new NoCardAbilities();
        var events = new List<GameEvent>();
        var order = Assert.IsType<Prompt>(Sequence.Work(world, Cards, abilities, events));
        Sequence.Answer(world, Cards, abilities, order, Decision.Take(first.ObjectId), events);
        var join = Assert.IsType<Prompt>(Sequence.Work(world, Cards, abilities, events));
        Sequence.Answer(world, Cards, abilities, join, Decision.Take(destination.Id), events);
        var conflict = Assert.IsType<Prompt>(Sequence.Work(world, Cards, abilities, events));
        Assert.Contains(conflict.Affordances, offer => offer.Id == unique.ObjectId);
        world.Join(PlayArea.Of(2), destination, "uncontrolled topology change", events);
        string before = KangPlayerCountFixture.Snapshot(world, events);

        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, Cards, abilities,
            conflict, Decision.Take(unique.ObjectId), events));
        Assert.Equal(before, KangPlayerCountFixture.Snapshot(world, events));
    }
}


