using Marvel.Godot;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class InteractionLifecycleTests
{
    [Fact]
    public void ADeferredOperationOnlyBelongsToItsIssuingRender()
    {
        var generation = new InteractionGeneration();

        int first = generation.Advance();
        int second = generation.Advance();

        Assert.False(generation.IsCurrent(first));
        Assert.True(generation.IsCurrent(second));
    }

    [Fact]
    public void OnePromptRevisionAcceptsOnlyOneMixedSubmission()
    {
        var latch = new PromptSubmissionLatch();

        latch.Render(7);

        Assert.True(latch.TrySubmit(7)); // Pointer click.
        Assert.False(latch.TrySubmit(7)); // Enter repeat.
        Assert.False(latch.TrySubmit(7)); // Mixed pointer/keyboard burst.
    }

    [Fact]
    public void ANewRevisionReopensButAnUncertainOneRemainsLatched()
    {
        var latch = new PromptSubmissionLatch();

        latch.Render(7);
        Assert.True(latch.TrySubmit(7));
        latch.Render(7);
        Assert.False(latch.TrySubmit(7));
        latch.Render(8);
        Assert.True(latch.TrySubmit(8));
    }

    [Fact]
    public void AProvenNotSentDecisionCanBeRetried()
    {
        var latch = new PromptSubmissionLatch();

        latch.Render(7);
        Assert.True(latch.TrySubmit(7));
        latch.AllowRetry(7);

        Assert.True(latch.TrySubmit(7));
    }

    [Fact]
    public void ARejectedAuthoritativeViewCanReopenItsSameRevision()
    {
        var latch = new PromptSubmissionLatch();

        latch.Render(7);
        Assert.True(latch.TrySubmit(7));
        latch.Render(7); // A synchronization alone must preserve the lock.
        Assert.False(latch.TrySubmit(7));
        latch.AllowRetry(7); // The engine explicitly rejected this decision.

        Assert.True(latch.TrySubmit(7));
    }
}
