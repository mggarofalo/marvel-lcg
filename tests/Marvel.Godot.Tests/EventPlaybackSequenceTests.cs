using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class EventPlaybackSequenceTests
{
    [Fact]
    public void MotionOffRetainsEveryOrderedResultForManualReview()
    {
        var playback = new EventPlaybackSequence();
        playback.Replace([Cue("boost"), Cue("damage"), Cue("defeat")], false);
        Assert.False(playback.Playing);
        Assert.Equal("defeat", playback.Current!.Summary);
        Assert.False(playback.Advance());
        playback.Move(-1);
        Assert.Equal("damage", playback.Current!.Summary);
        playback.Move(-1);
        Assert.Equal("boost", playback.Current!.Summary);
        playback.Move(1);
        Assert.Equal("damage", playback.Current!.Summary);
    }

    [Fact]
    public void NavigationPausesAndFinishingRetainsTheLastResult()
    {
        var playback = new EventPlaybackSequence();
        playback.Replace([Cue("one"), Cue("two"), Cue("three")], true);
        Assert.True(playback.Advance());
        playback.Move(-1);
        Assert.False(playback.Advance());
        Assert.Equal("one", playback.Current!.Summary);
        playback.Resume();
        Assert.True(playback.Advance());
        playback.Finish();
        Assert.False(playback.Playing);
        Assert.Equal("three", playback.Current!.Summary);
        playback.Resume();
        Assert.Equal("one", playback.Current!.Summary);
    }

    [Fact]
    public void ReplacementAndResetCannotRevivePriorResponseSubjects()
    {
        var playback = new EventPlaybackSequence();
        playback.Replace([Cue("old")], true);
        playback.Replace([Cue("new")], false);
        Assert.Equal(1, playback.Count);
        Assert.Equal("new", playback.Current!.Summary);
        playback.Clear();
        playback.Move(-1);
        playback.Resume();
        Assert.Null(playback.Current);
        Assert.Equal(0, playback.Count);
        Assert.False(playback.Playing);
    }

    [Fact]
    public void ReducedMotionAndCompletionPreferConsequencesOverBookkeeping()
    {
        var payment = Cue("discard payment");
        var damage = Cue("Rhino HP 14 → 12") with { Motion = EventMotionKind.Damage };
        var discard = Cue("discard event");
        var playback = new EventPlaybackSequence();
        playback.Replace([payment, damage, discard], false);
        Assert.Same(damage, playback.Current);
        playback.Resume();
        Assert.Same(payment, playback.Current);
        playback.Finish();
        Assert.Same(damage, playback.Current);
        playback.Move(1);
        Assert.Same(discard, playback.Current);
    }

    private static EventPresentation Cue(string name) => new(name, "Ability", [], EventMotionKind.State);
}
