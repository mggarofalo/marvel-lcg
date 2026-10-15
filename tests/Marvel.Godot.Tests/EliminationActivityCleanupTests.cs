using Marvel.Godot;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class EliminationActivityCleanupTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitEliminationCleanupKeepsEachLaterConsequenceAndTheFullAccount(bool terminal)
    {
        // Synthetic authorized facts model a defeated seat's removal before another seat's boost.
        EventPresentation damage = Quantity("Deal_Damage", "health", 3, 0, EventMotionKind.Damage);
        EventPresentation[] cleanup = [.. Enumerable.Range(1, 46).Select(Eliminated)];
        EventPresentation[] later = [
            new("An attachment detached", "", [], EventMotionKind.Move)
                { Activity = new([], []) { Operation = "Detach" } },
            new("Another seat's boost revealed", "", [], EventMotionKind.Flip)
                { Activity = new([], []) { Operation = "Boost" } },
            Quantity("Boost_Resolved", "boost_icons", 0, 2, EventMotionKind.State),
            Quantity("Place_Threat", "k_threat", 11, 14, EventMotionKind.Threat),
        ];
        EventPresentation[] ending = terminal
            ? [Quantity("Complete", "is_completed", 0, 1, EventMotionKind.State),
                new("Defeat", "Game outcome", [], EventMotionKind.Terminal)] : [];
        EventPresentation[] originals = [damage, .. cleanup, .. later, .. ending];
        var sequence = new EventActivitySequence();
        sequence.Start(originals, true);
        Assert.Same(damage, sequence.Current);
        sequence.Advance(1);
        EventPresentation grouped = sequence.Current!;
        Assert.Equal("Player elimination", grouped.Cause);
        Assert.Equal("Player elimination · 46 cards removed", grouped.Summary);
        Assert.Equal(cleanup, grouped.Details);
        Assert.Equal(cleanup, EventActivityCardFacts.Facts(grouped));
        Assert.Equal(46, grouped.Activity!.Subjects.Count);
        var remaining = new List<EventPresentation>();
        for (int beat = 0; beat < originals.Length && sequence.Active; beat++)
        {
            sequence.Advance(1);
            if (sequence.Active) remaining.Add(sequence.Current!);
        }
        Assert.False(sequence.Active);
        Assert.Equal(later.Concat(ending), remaining);
        Assert.Equal(originals, sequence.Entries);
        Assert.Equal(2 + later.Length + ending.Length, sequence.BeatCount);
    }

    [Fact]
    public void NonterminalOrdinaryMovementsAreNotReinterpretedAsElimination()
    {
        EventPresentation moved = Eliminated(1) with
            { Activity = Eliminated(1).Activity! with { Operation = "Remove" } };
        EventPresentation[] entries = [moved, moved,
            Quantity("Deal_Damage", "health", 3, 0, EventMotionKind.Damage)];
        Assert.Equal(entries, EventActivityBeats.From(entries));
    }

    [Fact]
    public void RepeatedEliminationReceiptsRetainTheirOrderAndOneSubjectPerPhysicalCard()
    {
        EventPresentation first = Eliminated(1);
        EventPresentation repeated = Eliminated(1);
        EventPresentation grouped = Assert.Single(EventActivityBeats.From([first, repeated]));
        Assert.Equal([first, repeated], grouped.Details);
        Assert.Equal(1, Assert.Single(grouped.Activity!.Subjects).Card);
        Assert.Equal([1], grouped.Anchors);
        Assert.Equal("Player elimination · 1 card removed", grouped.Summary);
    }

    [Fact]
    public void EliminationNeverGroupsAcrossStateStatusInstructionOrAbilityFacts()
    {
        EventPresentation first = Eliminated(1);
        EventPresentation second = Eliminated(2);
        EventPresentation[] boundaries = [
            Quantity("Eliminate", "health", 3, 0, EventMotionKind.Move),
            first with { Activity = first.Activity! with { StatusChange = ActivityStatusChange.Removed } },
            first with { Activity = first.Activity! with { Batch = 4 } },
            first with { Activity = first.Activity! with { PhaseStep = true } },
            first with { Activity = first.Activity! with { PhaseBoundary = true } },
            first with { Details = [first] },
            first with { ForcedAbilityNotification = new(1, "Authorized forced ability", "Resolved", 0, 1, true) },
            first with { Activity = first.Activity! with { Operation = "FutureEliminate" } },
        ];
        foreach (EventPresentation boundary in boundaries)
        {
            EventPresentation[] entries = [first, second, boundary, first, second];
            IReadOnlyList<EventPresentation> beats = EventActivityBeats.From(entries);
            Assert.Equal(3, beats.Count);
            Assert.Equal([first, second], beats[0].Details);
            Assert.Same(boundary, beats[1]);
            Assert.Equal([first, second], beats[2].Details);
            var sequence = new EventActivitySequence();
            sequence.Start(entries, false);
            Assert.False(sequence.Active);
            Assert.Equal(entries, sequence.Entries);
        }
    }

    private static EventPresentation Eliminated(int card) => new("An authorized card moved", "", [card], EventMotionKind.Move)
    {
        Activity = new([new(card, "Authorized card", ActivitySubjectRole.Subject)], [])
            { Operation = "Eliminate", From = "player deck", To = "removed" },
    };

    private static EventPresentation Quantity(string operation, string field, long before, long after, EventMotionKind motion) =>
        new("An authorized quantity changed", "", [], motion)
            { Activity = new([], [new(field, before, after)]) { Operation = operation } };
}
