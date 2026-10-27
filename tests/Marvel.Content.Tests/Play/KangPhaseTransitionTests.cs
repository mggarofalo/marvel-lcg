using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Prepared nonpublic Kang departure; no printed defeat hook or public scenario setup.
public sealed class KangPhaseTransitionTests : CoreCardTestBase
{
    private static readonly int[] JoinedSeats = [0, 1];
    [Theory]
    [Rule("rr:delayed-effect.1")]
    [Rule("rr:lasting-effects.5")]
    [InlineData(0, PhaseTransitionKind.EndPlayerPhase)]
    [InlineData(1, PhaseTransitionKind.EndPlayerPhase)]
    [InlineData(0, PhaseTransitionKind.EndVillainPhase)]
    [InlineData(1, PhaseTransitionKind.EndVillainPhase)]
    public void RequiredChoicesRetainTheExactPhaseOwnerAndCapturedGroup(int firstPlayer, PhaseTransitionKind boundary)
    {
        // 11002: "At the end of the phase, join another game area."
        // pack:mc11:rules-clarifications: discard matching uniques until one
        // remains; the first player decides if players cannot agree.
        var world = Hero("01001a", 3);
        world.FirstPlayer = firstPlayer;
        var source = world.CreateGameArea();
        var destination = world.CreateGameArea();
        world.Join(PlayArea.Of(1), source, "prepared split", []);
        world.Join(PlayArea.Of(0), source, "prepared split", []);
        world.Join(PlayArea.Of(2), destination, "prepared split", []);
        world.Detach(PlayArea.Villains, "prepared split", []);
        foreach (var seat in world.Seats)
        {
            World.MoveToTop(seat.IdentityCard, seat.Hand);
            world.CreateCard("01089", seat.Deck);
        }
        var kang = world.CreateCard("11002", world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)));
        var first = world.CreateCard("01011", world.Seats[0].Hero);
        var second = world.CreateCard("01011", world.Seats[1].Hero);
        var third = world.CreateCard("01011", world.Seats[2].Hero);
        var delayedDiscard = world.CreateCard("01041", world.Seats[2].Hero);
        var captured = world.Agenda.KangTransitions.CaptureJoin(world, kang, 7, boundary);
        int incarnation = kang.Incarnation;
        World.MoveToTop(kang, world.AreaOf(DeckType.EncounterDiscardPile));
        World.MoveToTop(kang, world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)));
        World.MoveToTop(kang, world.AreaOf(DeckType.RemovedArea));
        // rr:lasting-effects.5: effects lasting until the end of the phase expire.
        // rr:delayed-effect.1: delayed effects resolve before responses may be used.
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "attack", 2, first.ObjectId, first.ObjectId, Duration.UntilEndOf(TimingPoints.EndOfPhase)));
        world.Effects.Register(new ContinuousEffect(EffectSource.DelayedEffect,
            DelayedEffects.DiscardFromPlay, Card: delayedDiscard.ObjectId, Affects: delayedDiscard.ObjectId,
            Lasts: Duration.NextTime(boundary == PhaseTransitionKind.EndPlayerPhase
                ? PhaseEnd.PlayerPhaseEnds : PhaseEnd.VillainPhaseEnds)));
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(boundary), 7, 6));
        var owner = world.Agenda.Current!.Value;
        var origin = world.Agenda.Occurrence!;
        // An identical future phase frame distinguishes exact ownership from
        // matching a timing name, coordinates, or occurrence alone.
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(boundary), 7, 6));
        long words = world.Random.Generator.WordsConsumed;
        var abilities = new NoCardAbilities();
        var events = new List<GameEvent>();

        var prompt = Sequence.Work(world, Cards, abilities, events)!;

        Assert.Equal(firstPlayer, prompt.Player);
        Assert.False(prompt.Cancellable);
        Assert.DoesNotContain(world.Effects.Active(), effect => effect.Source == EffectSource.LastingEffect);
        Assert.NotEqual(world.Seats[2].Hero, delayedDiscard.Area);
        Assert.Single(prompt.Affordances);
        Assert.Equal(destination.Id, prompt.Affordances[0].Id);
        var child = world.Agenda.Current!.Value.Require<KangJoinOperation>();
        Assert.Equal(owner, child.Owner);
        Assert.Same(origin, child.Origin);
        Assert.True(world.Agenda.IsOutstanding(owner, origin));
        Assert.Equal(new[] { PlayArea.Of(0), PlayArea.Of(1) }, captured.Group);
        Assert.Equal(incarnation, captured.Defeated.Incarnation);
        Assert.NotEqual(kang.Incarnation, captured.Defeated.Incarnation);
        Assert.Empty(world.Agenda.KangTransitions.Pending);
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, abilities, prompt, Decision.Decline, events));
        Assert.Same(source, world.GameAreaOf(PlayArea.Of(0)));

        Sequence.Answer(world, Cards, abilities, prompt, Decision.Take(destination.Id), events);
        prompt = Sequence.Work(world, Cards, abilities, events)!;
        Assert.True(world.Agenda.IsOutstanding(owner, origin));
        Assert.Equal(new[] { first.ObjectId, second.ObjectId, third.ObjectId }, prompt.Affordances.Select(offer => offer.Id));
        Sequence.Answer(world, Cards, abilities, prompt, Decision.Take(second.ObjectId), events);
        prompt = Sequence.Work(world, Cards, abilities, events)!;
        Assert.Equal(new[] { first.ObjectId, third.ObjectId }, prompt.Affordances.Select(offer => offer.Id));
        Assert.True(world.Agenda.IsOutstanding(owner, origin));
        Assert.Same(source, world.GameAreaOf(PlayArea.Of(1)));
        Sequence.Answer(world, Cards, abilities, prompt, Decision.Take(first.ObjectId), events);

        Assert.Null(Sequence.Work(world, Cards, abilities, events));
        Assert.False(world.Agenda.IsOutstanding(owner, origin));
        Assert.False(world.Agenda.IsBusy);
        Assert.Same(destination, world.GameAreaOf(PlayArea.Of(0)));
        Assert.Same(destination, world.GameAreaOf(PlayArea.Of(1)));
        Assert.Equal(JoinedSeats, events.OfType<PlayAreaJoined>().Select(item => item.PlayArea));
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Contains(third, world.Seats[2].Hero.Cards);
        Assert.Null(Sequence.Work(world, Cards, abilities, events));
        Assert.Equal(2, events.OfType<PlayAreaJoined>().Count());
    }
}
