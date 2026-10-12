using Marvel.Godot;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class EventActivitySequenceTests
{
    [Fact]
    public void SlowFramesDoNotSkipCommittedConsequences()
    {
        // Synthetic visibility-safe response: the animation consumes descriptors, not game state.
        EventPresentation[] entries = [Cue("Threat increased."), Cue("A boost resolved."), Cue("Rhino schemed.")];
        var sequence = new EventActivitySequence();
        sequence.Start(entries, animate: true);
        Assert.Equal(entries[0], sequence.Current);
        Assert.False(sequence.Advance(0.1));
        Assert.True(sequence.Advance(60));
        Assert.Equal(entries[1], sequence.Current);
        Assert.True(sequence.Active);
        Assert.False(sequence.Advance(0.1));
        sequence.Advance(60);
        Assert.Equal(entries[2], sequence.Current);
        Assert.True(sequence.Active);
        sequence.Advance(60);
        Assert.False(sequence.Active);
        Assert.Equal(entries, sequence.Entries);
    }

    [Fact]
    public void ReducedMotionRetainsEveryOrderedResultWithoutWaiting()
    {
        EventPresentation[] entries = [Cue("Threat increased."), Cue("Rhino schemed.")];
        var sequence = new EventActivitySequence();
        sequence.Start(entries, animate: false);
        Assert.False(sequence.Active);
        Assert.Equal(entries, sequence.Entries);
        Assert.False(sequence.Advance(10));
        sequence.Start(entries, animate: true);
        sequence.Complete();
        Assert.False(sequence.Active);
        Assert.Equal(entries, sequence.Entries);
    }

    [Fact]
    public void ReplacementAndSynchronizationDiscardPendingActivity()
    {
        var sequence = new EventActivitySequence();
        sequence.Start([Cue("Old damage."), Cue("Old boost.")], animate: true);
        EventPresentation replacement = Cue("New result.");
        sequence.Start([replacement], animate: true);
        Assert.Equal(replacement, sequence.Current);
        Assert.Single(sequence.Entries);
        sequence.Clear();
        Assert.Null(sequence.Current);
        Assert.Empty(sequence.Entries);
        Assert.False(sequence.Active);
        Assert.False(sequence.Advance(100));
    }

    [Theory]
    [InlineData(1, 0.5)]
    [InlineData(4, 2.6)]
    [InlineData(30, 3.0)]
    public void PlaybackBudgetKeepsDecisionsResponsiveWithoutDroppingBeats(int count, double limit)
    {
        var sequence = new EventActivitySequence();
        sequence.Start(Enumerable.Range(0, count).Select(index => Cue($"Event {index}")).ToArray(), true);
        var observed = new HashSet<int>();
        double seconds = 0;
        while (sequence.Active && seconds < limit)
        {
            observed.Add(sequence.Index);
            sequence.Advance(1.0 / 120);
            seconds += 1.0 / 120;
        }
        Assert.False(sequence.Active);
        Assert.Equal(count, observed.Count);
    }

    private static EventPresentation Cue(string summary) => new(summary, "Result", [], EventMotionKind.Threat);
}
