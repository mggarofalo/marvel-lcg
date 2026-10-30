using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Prepared capability review; no printed Kang admission or scenario setup.
public sealed class KangPhaseClosureReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData(PhaseTransitionKind.EndPlayerPhase, PhaseEnd.PlayerPhaseEnds)]
    [InlineData(PhaseTransitionKind.EndVillainPhase, PhaseEnd.VillainPhaseEnds)]
    [InlineData(PhaseTransitionKind.EndVillainPhase, PhaseEnd.RoundEnds)]
    public void RepeatedRefusalPreservesDelayedUseCountAndOriginalDeparture(
        PhaseTransitionKind boundary, string condition)
    {
        var world = Hero("01001a,01001b", 2);
        var source = world.CreateGameArea();
        var destination = world.CreateGameArea();
        world.Join(PlayArea.Of(0), source, "prepared split", []);
        world.Join(PlayArea.Of(1), destination, "prepared split", []);
        world.Detach(PlayArea.Villains, "prepared split", []);
        var kang = world.CreateCard("11002", world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)));
        var target = world.CreateCard("01011", world.Seats[0].Hero);
        var departure = world.Agenda.KangTransitions.CaptureJoin(world, kang, 7, boundary);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "attack", 2, target.ObjectId, target.ObjectId,
            Duration.UntilEndOf(TimingPoints.EndOfPhase)));
        world.Effects.Register(new ContinuousEffect(EffectSource.DelayedEffect,
            DelayedEffects.DiscardFromPlay, Card: target.ObjectId, Affects: target.ObjectId,
            Lasts: Duration.NextTime(condition, times: 2)));
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(boundary), 7, 6));
        var registered = world.Effects.Registered.ToArray();
        string digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var events = new List<GameEvent>();

        for (int attempt = 0; attempt < 2; attempt++)
        {
            Assert.Throws<RulesNotImplementedException>(() =>
                Sequence.Work(world, Cards, new NoCardAbilities(), events));
            Assert.Equal(registered, world.Effects.Registered);
            Assert.Equal(digest, world.Digest().Canonical());
            Assert.Equal(words, world.Random.Generator.WordsConsumed);
            Assert.Same(departure, Assert.Single(world.Agenda.KangTransitions.Pending));
            Assert.Same(source, world.GameAreaOf(PlayArea.Of(0)));
            Assert.Same(destination, world.GameAreaOf(PlayArea.Of(1)));
            Assert.Empty(events);
        }

        // A two-use registration must still survive its first actual occurrence.
        // This observes retained remaining uses, which Registered alone omits.
        DelayedEffects.Occur(world, condition, events);
        Assert.Single(world.Effects.Registered, effect => effect.Source == EffectSource.DelayedEffect);
        Assert.NotSame(world.Seats[0].Hero, target.Area);
        DelayedEffects.Occur(world, condition, events);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Source == EffectSource.DelayedEffect);
    }
}
