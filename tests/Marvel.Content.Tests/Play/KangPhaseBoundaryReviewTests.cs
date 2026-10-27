using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Prepared engine capability probes; these do not admit the Kang scenario.
public sealed class KangPhaseBoundaryReviewTests : CoreCardTestBase
{
    [Fact]
    public void AddedSourceMemberMustFailClosedRatherThanLeavePartOfTheGroupBehind()
    {
        var (world, kang, source, _) = Split();
        world.Agenda.KangTransitions.CaptureJoin(world, kang, 7, PhaseTransitionKind.EndPlayerPhase);
        world.Join(PlayArea.Of(2), source, "prepared group change", []);
        AddBoundary(world);

        Assert.Throws<RulesNotImplementedException>(() =>
            Sequence.Work(world, Cards, new NoCardAbilities(), []));
    }

    [Fact]
    [Rule("rr:delayed-effect.1")]
    public void CompetingDelayedUniqueDiscardMustFailClosedWithoutAnAcceptedOrderingPolicy()
    {
        // Delayed effects resolve "before responses to that point or condition
        // may be used"; this does not decide their order relative to each other.
        var (world, kang, _, _) = Split();
        var first = world.CreateCard("01011", world.Seats[0].Hero);
        world.CreateCard("01011", world.Seats[1].Hero);
        world.Agenda.KangTransitions.CaptureJoin(world, kang, 7, PhaseTransitionKind.EndPlayerPhase);
        world.Effects.Register(new ContinuousEffect(EffectSource.DelayedEffect,
            DelayedEffects.DiscardFromPlay, Card: first.ObjectId, Affects: first.ObjectId,
            Lasts: Duration.NextTime(PhaseEnd.PlayerPhaseEnds)));
        AddBoundary(world);

        Assert.Throws<RulesNotImplementedException>(() =>
            Sequence.Work(world, Cards, new NoCardAbilities(), []));
    }

    [Fact]
    public void SimultaneousDeparturesFailClosed()
    {
        var (world, kang, _, _) = Split();
        var other = world.CreateCard("11003", world.AreaOf(DeckType.VillainArea, PlayArea.Of(1)));
        world.Agenda.KangTransitions.CaptureJoin(world, kang, 7, PhaseTransitionKind.EndPlayerPhase);
        world.Agenda.KangTransitions.CaptureJoin(world, other, 7, PhaseTransitionKind.EndPlayerPhase);
        AddBoundary(world);

        var error = Assert.Throws<RulesNotImplementedException>(() =>
            Sequence.Work(world, Cards, new NoCardAbilities(), []));

        Assert.Contains("simultaneous", error.Message);
        Assert.Equal(2, world.Agenda.KangTransitions.Pending.Count);
    }

    [Fact]
    public void LastLocalGroupFailsClosed()
    {
        var (world, kang, source, _) = Split();
        world.Join(PlayArea.Of(1), source, "prepared last area", []);
        world.Join(PlayArea.Of(2), source, "prepared last area", []);
        world.Agenda.KangTransitions.CaptureJoin(world, kang, 7, PhaseTransitionKind.EndPlayerPhase);
        AddBoundary(world);

        var error = Assert.Throws<RulesNotImplementedException>(() =>
            Sequence.Work(world, Cards, new NoCardAbilities(), []));

        Assert.Contains("central", error.Message);
        Assert.All(Enumerable.Range(0, world.Players), seat => Assert.Same(source, world.GameAreaOf(PlayArea.Of(seat))));
    }

    private static (World World, Card Kang, GameArea Source, GameArea Destination) Split()
    {
        var world = Hero("01001a,01001b", 3);
        var source = world.CreateGameArea();
        var destination = world.CreateGameArea();
        world.Join(PlayArea.Of(0), source, "prepared split", []);
        world.Join(PlayArea.Of(1), destination, "prepared split", []);
        world.Detach(PlayArea.Villains, "prepared split", []);
        foreach (var seat in world.Seats)
        {
            World.MoveToTop(seat.IdentityCard, seat.Hand);
            world.CreateCard("01089", seat.Deck);
        }
        var kang = world.CreateCard("11002", world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)));
        return (world, kang, source, destination);
    }

    private static void AddBoundary(World world) =>
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerPhase), 7, 6));
}
