using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class ForcedAbilityQueueTests
{
    [Fact]
    public void ReceiptsStayInOrderUntilAcknowledgedAndSynchronizationDoesNotRepeatThem()
    {
        // Synthetic authorized receipts test presentation lifetime, not a played Core sequence.
        var queue = new ForcedAbilityQueue();
        EventPresentation first = Receipt(1, true);
        EventPresentation second = Receipt(2, true);
        queue.Record("game", 4, [first, second], accepted: true, reset: false);
        queue.Record("game", 4, [first, second], accepted: true, reset: false);
        Assert.Equal(2, queue.Count);
        Assert.Equal(first.ForcedAbilityNotification, queue.Current);
        queue.Record("game", 5, [], accepted: false, reset: false);
        Assert.Equal(2, queue.Count);
        queue.Acknowledge();
        Assert.Equal(second.ForcedAbilityNotification, queue.Current);
        queue.Acknowledge();
        Assert.Null(queue.Current);
        queue.Record("game", 4, [first, second], accepted: true, reset: false);
        Assert.Equal(0, queue.Count);
    }

    [Fact]
    public void MandatoryDecisionAcknowledgesOnlyAfterANewerAcceptedResponse()
    {
        var queue = new ForcedAbilityQueue();
        queue.Record("game", 4, [Receipt(1, false)], accepted: true, reset: false);
        Prompt decision = RequiredPrompt(1);
        Assert.Equal(queue.Current, queue.RequiredDecision(decision, 4));
        queue.Record("game", 5, [], accepted: false, reset: false);
        Assert.Equal(1, queue.Count);
        Assert.Equal(queue.Current, queue.RequiredDecision(decision, 4));
        queue.Record("game", 5, [], accepted: true, reset: false);
        Assert.Null(queue.Current);
    }

    [Fact]
    public void SuspendedReceiptCannotClaimAnotherOrOptionalDecisionAsItsAcknowledgement()
    {
        var queue = new ForcedAbilityQueue();
        queue.Record("game", 4, [Receipt(1, false)], accepted: true, reset: false);
        Assert.Null(queue.RequiredDecision(RequiredPrompt(2), 4));
        Assert.Null(queue.RequiredDecision(RequiredPrompt(1) with { Cancellable = true }, 4));
        queue.Record("game", 5, [], accepted: true, reset: false);
        Assert.Equal(1, queue.Count);
    }

    [Fact]
    public void UndoAndNewSessionDiscardReceiptsWithoutReplayingThem()
    {
        var queue = new ForcedAbilityQueue();
        queue.Record("game", 4, [Receipt(1, true)], accepted: true, reset: false);
        queue.Record("game", 5, [], accepted: false, reset: true);
        Assert.Null(queue.Current);
        queue.Record("other", 1, [], accepted: false, reset: false);
        Assert.Null(queue.Current);
    }

    private static Prompt RequiredPrompt(int source) =>
        new(0, Question.Opportunity, TimingPriority.ForcedResponse, "synthetic", "Required choice", false, [])
        { ContextCardIds = [source] };

    private static EventPresentation Receipt(int source, bool completed) =>
        new("Known effect", "Synthetic forced ability", [source], EventMotionKind.State)
        {
            ForcedAbilityNotification = new ForcedAbilityNotification(source, "Source Forced Response",
                "Known effect", 0, source, completed),
        };
}
