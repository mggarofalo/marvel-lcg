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
    private static readonly int[] OrderOffers = [0, 1];

    [Fact]
    public void AddedSourceMemberMustFailClosedRatherThanLeavePartOfTheGroupBehind()
    {
        var (world, kang, source, _) = Split();
        var departure = world.Agenda.KangTransitions.CaptureJoin(world, kang, 7, PhaseTransitionKind.EndPlayerPhase);
        world.Join(PlayArea.Of(2), source, "prepared group change", []);
        AddBoundary(world);
        var events = new List<GameEvent>();
        string digest = world.Digest().Canonical();

        Assert.Throws<RulesNotImplementedException>(() =>
            Sequence.Work(world, Cards, new NoCardAbilities(), events));

        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Same(departure, Assert.Single(world.Agenda.KangTransitions.Pending));
        Assert.Equal(2, source.PlayAreas.Count);
        Assert.Empty(events);
    }

    [Theory]
    [Rule("rr:first-player.3")]
    [InlineData(PhaseTransitionKind.EndPlayerPhase, PhaseEnd.PlayerPhaseEnds, false)]
    [InlineData(PhaseTransitionKind.EndVillainPhase, PhaseEnd.VillainPhaseEnds, false)]
    [InlineData(PhaseTransitionKind.EndVillainPhase, PhaseEnd.RoundEnds, false)]
    [InlineData(PhaseTransitionKind.EndPlayerPhase, PhaseEnd.PlayerPhaseEnds, true)]
    [InlineData(PhaseTransitionKind.EndVillainPhase, PhaseEnd.RoundEnds, true)]
    public void CompetingDelayedDiscardResolvesInTheFirstPlayersOrder(
        PhaseTransitionKind boundary, string condition, bool kangFirst)
    {
        // "If two or more effects would resolve simultaneously, the first player
        // decides the order in which to resolve them" (MARVEL-894 Q6). The order
        // matters here: discarding the unique copy first avoids the join conflict.
        var (world, kang, _, destination) = Split();
        var first = world.CreateCard("01011", world.Seats[0].Hero);
        var other = world.CreateCard("01011", world.Seats[1].Hero);
        world.Agenda.KangTransitions.CaptureJoin(world, kang, 7, boundary);
        world.Effects.Register(new ContinuousEffect(EffectSource.DelayedEffect,
            DelayedEffects.DiscardFromPlay, Card: first.ObjectId, Affects: first.ObjectId,
            Lasts: Duration.NextTime(condition)));
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(boundary), 7, 6));
        var abilities = new NoCardAbilities();
        var events = new List<GameEvent>();

        var order = Sequence.Work(world, Cards, abilities, events)!;

        Assert.Equal(KangPhaseEndOrderOperation.Order, order.Trigger);
        Assert.Equal(world.FirstPlayer, order.Player);
        Assert.Equal(OrderOffers, order.Affordances.Select(offer => offer.Id));
        Assert.StartsWith($"{EffectiveCards.Title(kang, world.Facts)} was defeated, so ", order.Affordances[0].Description,
            StringComparison.Ordinal);
        Assert.Same(world.Seats[0].Hero, first.Area);
        Sequence.Answer(world, Cards, abilities, order, Decision.Take(kangFirst ? 0 : 1), events);
        Assert.Equal(kangFirst, ReferenceEquals(world.Seats[0].Hero, first.Area));

        var join = Sequence.Work(world, Cards, abilities, events)!;
        Assert.All(join.Affordances, offer => Assert.Equal("Join_Game_Area", offer.Verb));
        Sequence.Answer(world, Cards, abilities, join, Decision.Take(destination.Id), events);
        var conflict = Sequence.Work(world, Cards, abilities, events);
        if (kangFirst)
        {
            Assert.NotNull(conflict);
            Assert.All(conflict.Affordances, offer => Assert.Equal("Discard_Joined_Unique", offer.Verb));
            Sequence.Answer(world, Cards, abilities, conflict, Decision.Take(other.ObjectId), events);
            Assert.Same(world.Seats[0].Hero, first.Area);
            Assert.Null(Sequence.Work(world, Cards, abilities, events));
        }
        else
        {
            Assert.Null(conflict);
            Assert.Same(world.Seats[1].Hero, other.Area);
        }

        Assert.NotSame(world.Seats[0].Hero, first.Area);
        Assert.Same(destination, world.GameAreaOf(PlayArea.Of(0)));
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Source == EffectSource.DelayedEffect);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void UnmatchedDelayedRegistrationsDoNotBlockTheDeparture(int mismatch)
    {
        var (world, kang, _, _) = Split();
        var target = world.CreateCard("01041", world.Seats[0].Hero);
        world.Agenda.KangTransitions.CaptureJoin(world, kang, 7, PhaseTransitionKind.EndPlayerPhase);
        world.Effects.Register(new ContinuousEffect(EffectSource.DelayedEffect,
            DelayedEffects.DiscardFromPlay, Card: target.ObjectId, Affects: target.ObjectId,
            Lasts: Duration.NextTime(mismatch == 0 ? Steps.AttackEnds : PhaseEnd.PlayerPhaseEnds))
        {
            ActivationId = mismatch == 1 ? 123 : null,
            AwaitingActivation = mismatch == 2 ? 456 : null,
        });
        AddBoundary(world);
        var registered = world.Effects.Registered.ToArray();

        var prompt = Sequence.Work(world, Cards, new NoCardAbilities(), [])!;

        Assert.NotEmpty(prompt.Affordances);
        Assert.All(prompt.Affordances, offer => Assert.Equal("Join_Game_Area", offer.Verb));
        Assert.Equal(registered, world.Effects.Registered);
        Assert.Same(world.Seats[0].Hero, target.Area);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GroupChangesWhileChoosingCannotCommitAnAnswer(bool destinationChosen)
    {
        var (world, kang, source, destination) = Split();
        var first = world.CreateCard("01011", world.Seats[0].Hero);
        world.CreateCard("01011", world.Seats[1].Hero);
        world.Agenda.KangTransitions.CaptureJoin(world, kang, 7, PhaseTransitionKind.EndPlayerPhase);
        AddBoundary(world);
        var abilities = new NoCardAbilities();
        var prompt = Sequence.Work(world, Cards, abilities, [])!;
        if (destinationChosen)
        {
            Sequence.Answer(world, Cards, abilities, prompt, Decision.Take(destination.Id), []);
            prompt = Sequence.Work(world, Cards, abilities, [])!;
        }
        world.Join(PlayArea.Of(2), source, "prepared group change", []);
        string digest = world.Digest().Canonical();
        var events = new List<GameEvent>();

        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, Cards, abilities,
            prompt, Decision.Take(destinationChosen ? first.ObjectId : destination.Id), events));
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Work(world, Cards, abilities, events));

        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Same(world.Seats[0].Hero, first.Area);
        Assert.Same(source, world.GameAreaOf(PlayArea.Of(0)));
        Assert.Same(source, world.GameAreaOf(PlayArea.Of(2)));
        Assert.Same(destination, world.GameAreaOf(PlayArea.Of(1)));
        Assert.Empty(events);
    }

    [Fact]
    [Rule("rr:first-player.3")]
    public void SimultaneousDeparturesRequireTheFirstPlayersOrder()
    {
        var (world, kang, _, _) = Split();
        var other = world.CreateCard("11003", world.AreaOf(DeckType.VillainArea, PlayArea.Of(1)));
        world.Agenda.KangTransitions.CaptureJoin(world, kang, 7, PhaseTransitionKind.EndPlayerPhase);
        world.Agenda.KangTransitions.CaptureJoin(world, other, 7, PhaseTransitionKind.EndPlayerPhase);
        AddBoundary(world);

        // "If two or more effects would resolve simultaneously, the first player
        // decides the order in which to resolve them."
        var prompt = Assert.IsType<Marvel.Rules.Prompts.Prompt>(
            Sequence.Work(world, Cards, new NoCardAbilities(), []));

        Assert.Equal(KangDepartureOrderOperation.Order, prompt.Trigger);
        Assert.Equal(world.FirstPlayer, prompt.Player);
        Assert.Equal(new[] { kang.ObjectId, other.ObjectId }, prompt.Affordances.Select(offer => offer.Id));
        Assert.Empty(world.Agenda.KangTransitions.Pending);
    }

    [Fact]
    [Rule("rr:first-player.3")]
    public void KangFirstResolvesEveryDepartureBeforeACompetingDelayedEffect()
    {
        // MARVEL-894 Q6: with Kang's effect chosen first, both departures and their
        // joins resolve completely before the other delayed effect due now.
        var (world, kang, _, _) = Split();
        var other = world.CreateCard("11003", world.AreaOf(DeckType.VillainArea, PlayArea.Of(1)));
        var target = world.CreateCard("01041", world.Seats[2].Hero);
        world.Agenda.KangTransitions.CaptureJoin(world, kang, 7, PhaseTransitionKind.EndPlayerPhase);
        world.Agenda.KangTransitions.CaptureJoin(world, other, 7, PhaseTransitionKind.EndPlayerPhase);
        world.Effects.Register(new ContinuousEffect(EffectSource.DelayedEffect,
            DelayedEffects.DiscardFromPlay, Card: target.ObjectId, Affects: target.ObjectId,
            Lasts: Duration.NextTime(PhaseEnd.PlayerPhaseEnds)));
        AddBoundary(world);
        var abilities = new NoCardAbilities();
        var events = new List<GameEvent>();

        var order = Sequence.Work(world, Cards, abilities, events)!;
        Assert.Equal(KangPhaseEndOrderOperation.Order, order.Trigger);
        Sequence.Answer(world, Cards, abilities, order, Decision.Take(0), events);
        var departure = Sequence.Work(world, Cards, abilities, events)!;
        Assert.Equal(KangDepartureOrderOperation.Order, departure.Trigger);
        Sequence.Answer(world, Cards, abilities, departure, Decision.Take(kang.ObjectId), events);

        var prompt = Sequence.Work(world, Cards, abilities, events);
        for (int step = 0; prompt is not null; step++)
        {
            Assert.True(step < 10, prompt.Description);
            Assert.Equal(KangJoinOperation.Join, prompt.Trigger);
            Assert.Equal(DeckType.HeroArea, target.Area.Type);
            Sequence.Answer(world, Cards, abilities, prompt, Decision.Take(prompt.Affordances[0].Id), events);
            prompt = Sequence.Work(world, Cards, abilities, events);
        }

        int discarded = events.FindIndex(happened => happened is CardsMoved moved
            && moved.Cards.Any(landing => landing.Card == target.ObjectId));
        Assert.Equal(2, events.Count(happened => happened is PlayAreaJoined));
        Assert.True(discarded > events.FindLastIndex(happened => happened is PlayAreaJoined));
        Assert.NotEqual(DeckType.HeroArea, target.Area.Type);
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
