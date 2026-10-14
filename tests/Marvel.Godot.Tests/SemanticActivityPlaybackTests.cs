using Marvel.Godot;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class SemanticActivityPlaybackTests
{
    // Synthetic receipt contracts isolate causal ownership and presentation grouping.
    [Fact]
    public void OneAcceptedBackflipCannotOwnLaterSchemeOrTheNextRhinoAttack()
    {
        var first = new AttackIdentity(4, true);
        var second = new AttackIdentity(5, true);
        EventPresentation start = Boundary(first, true);
        EventPresentation finish = Boundary(first, false);
        EventPresentation next = Boundary(second, true);
        EventPresentation damage = Entry(2, EventActivityKind.Consequence) with
        { Motion = EventMotionKind.Damage, Activity = new([new(2, "Spider-Man", ActivitySubjectRole.Subject)], []) { AttackIdentity = first } };
        EventPresentation scheme = Entry(3, EventActivityKind.Consequence) with { Motion = EventMotionKind.Threat };
        var receipt = new DecisionReceiptContext("Backflip: Play Backflip", [9]) { ActionVerb = "Play", SourceCardId = 9 };
        var batch = new EventBatchPresentation([start, damage, finish, scheme, next], [start, damage, finish, scheme, next], []);
        EventPresentation[] cues = BoardResponsePresentation.Cues(batch, receipt);
        IReadOnlyList<EventPresentation> playback = EventPlaybackPlan.From(cues, receipt);
        Assert.Equal("Rhino → Spider-Man", playback[1].Intention);
        Assert.Null(playback[3].Intention);
        Assert.Equal(cues.Length, AcceptedActivityOccurrence.From(cues, receipt).Count);
        EventPresentation? active = null;
        active = AttackActivityPresentation.Advance(active, start);
        Assert.Same(start, active);
        Assert.Same(start, AttackActivityPresentation.Advance(active, damage));
        active = AttackActivityPresentation.Advance(active, finish);
        Assert.Null(active);
        Assert.Null(AttackActivityPresentation.Advance(active, scheme));
        active = AttackActivityPresentation.Advance(active, next);
        Assert.Same(next, active);
        Assert.Same(next, AttackActivityPresentation.Advance(active, finish));
    }

    [Fact]
    public void BoostTravelRevealAndDiscardHaveOneReadableBeatWithEveryReceiptInOrder()
    {
        EventPresentation[] receipts = [Entry(7, EventActivityKind.Travel), Entry(7, EventActivityKind.Reveal),
            Entry(7, EventActivityKind.Boost), Entry(7, EventActivityKind.Travel), Entry(3, EventActivityKind.Consequence)];
        IReadOnlyList<EventPresentation> beats = EventActivityBeats.From(receipts);
        Assert.Equal(2, beats.Count);
        Assert.Equal(EventActivityKind.Boost, beats[0].Activity!.Kind);
        Assert.Equal(receipts.Take(4), EventActivityCardFacts.Facts(beats[0]));
        Assert.Same(receipts[4], beats[1]);
        Assert.InRange(EventActivityTiming.Duration(beats[0]), .75, 1);
        Assert.True(beats.Sum(EventActivityTiming.Duration) < receipts.Sum(EventActivityTiming.Duration));
    }

    [Fact]
    public void GroupingNeverSwallowsForcedAcknowledgementsOrUnrelatedCards()
    {
        EventPresentation reveal = Entry(7, EventActivityKind.Reveal);
        EventPresentation mandatory = Entry(7, EventActivityKind.Travel) with
        { ForcedAbilityNotification = new(7, "Forced response", "Resolve this", 0, 3, true) };
        EventPresentation other = Entry(8, EventActivityKind.Travel);
        Assert.Equal(new[] { reveal, mandatory, other }, EventActivityBeats.From([reveal, mandatory, other]));
        var first = Entry(7, EventActivityKind.Travel);
        first = first with { Activity = first.Activity! with { AttackIdentity = new(1, true) } };
        reveal = reveal with { Activity = reveal.Activity! with { AttackIdentity = new(2, true) } };
        Assert.Equal(new[] { first, reveal }, EventActivityBeats.From([first, reveal]));
    }

    [Fact]
    public void TopologyRemainsInRawReceiptsWithoutBecomingGameplayCopy()
    {
        var topology = Entry(1, EventActivityKind.Topology) with { Summary = "play area left game area 0" };
        EventPresentation outcome = new("The players lost the game.", "", [], EventMotionKind.Terminal);
        var sequence = new EventActivitySequence();
        sequence.Start([topology, outcome], true);
        Assert.Equal(new[] { topology, outcome }, sequence.Entries);
        Assert.Equal(1, sequence.BeatCount);
        Assert.Same(outcome, sequence.Current);
        var grouped = outcome with { Details = [topology, outcome] };
        Assert.Equal(new[] { outcome }, Assert.Single(EventActivityBeats.From([grouped])).Details);
        Assert.Equal(new[] { topology, outcome }, grouped.Details);
    }

    [Theory]
    [InlineData("AlliesArea")]
    [InlineData("SupportsArea")]
    [InlineData("UpgradesArea")]
    [InlineData("EngagedEnemiesArea")]
    [InlineData("SideSchemesArea")]
    public void ArrivalsRequireTheAuthorizedDestination(string zone)
    {
        Assert.True(EventActivityTableArrivals.Arrives(new([], []) { DestinationZone = zone }, zone));
        Assert.False(EventActivityTableArrivals.Arrives(new([], []) { DestinationZone = "DiscardPile" }, zone));
        Assert.False(EventActivityTableArrivals.Arrives(new([], []) { DestinationZone = "PlayerDeck" }, "PlayerDeck"));
    }

    private static EventPresentation Entry(int card, EventActivityKind kind) => new(kind.ToString(), "", [card], EventMotionKind.State)
    { Activity = new([new(card, "Card", kind == EventActivityKind.Boost ? ActivitySubjectRole.Boost : ActivitySubjectRole.Subject)], []) { Kind = kind } };

    private static EventPresentation Boundary(AttackIdentity id, bool started) =>
        EventPresenter.Present(new AttackBoundary(id, 1, 2, started)
        {
            AttackIdentity = id,
            Verb = started ? "Attack_Initiated" : "Attack_Finished",
            Subjects = new Dictionary<int, string> { [1] = "Rhino", [2] = "Spider-Man" },
        }, new([], [], [], Outcome.Unfinished));
}
