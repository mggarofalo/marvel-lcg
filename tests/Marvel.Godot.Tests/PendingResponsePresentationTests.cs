using Xunit;

namespace Marvel.Godot.Tests;

public sealed class PendingResponsePresentationTests
{
    [Fact]
    public void AcceptedBoardAndNextDecisionApplyOnlyAfterPlaybackAndOnlyOnce()
    {
        var pending = new PendingResponsePresentation();
        string board = "Mockingbird defending";
        string? decision = null;
        int generation = pending.Begin(() =>
        {
            Assert.False(pending.Pending);
            board = "Mockingbird discarded";
            decision = "Spider-Sense for Stampede";
        });
        Assert.Equal("Mockingbird defending", board);
        Assert.Null(decision);
        Assert.True(pending.Pending);
        Assert.True(pending.Complete(generation));
        Assert.Equal("Mockingbird discarded", board);
        Assert.Equal("Spider-Sense for Stampede", decision);
        Assert.False(pending.Complete(generation));
    }

    [Theory]
    [InlineData("Sync")]
    [InlineData("Undo")]
    [InlineData("Session reset")]
    public void SupersedingAuthorityInvalidatesQueuedPlaybackCompletion(string replacement)
    {
        var pending = new PendingResponsePresentation();
        string board = "previous";
        int generation = pending.Begin(() => board = "stale accepted response");
        pending.Cancel();
        board = replacement;
        Assert.False(pending.Complete(generation));
        Assert.Equal(replacement, board);
        Assert.False(pending.Pending);
    }

    [Fact]
    public void EarlierCompletionCannotRevealANewerPendingPrompt()
    {
        var pending = new PendingResponsePresentation();
        int applied = 0;
        int earlier = pending.Begin(() => applied = 1);
        int latest = pending.Begin(() => applied = 2);
        Assert.False(pending.Complete(earlier));
        Assert.True(pending.Pending);
        Assert.Equal(0, applied);
        Assert.True(pending.Complete(latest));
        Assert.Equal(2, applied);
    }
}
