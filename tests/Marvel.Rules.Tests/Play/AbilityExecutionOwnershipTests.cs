using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class AbilityExecutionOwnershipTests
{
    [Fact]
    public void ARegisteredChoicePreventsItsOwnerFromAdvancing()
    {
        // Invocation identities and checked transitions are engine choices.
        var agenda = AgendaWithOwner();
        var owner = agenda.Current!.Value;
        var occurrence = agenda.Occurrence!;
        var invocation = agenda.Executions.Begin(occurrence, new(7, AbilityType.ForcedResponse, 0));
        ScheduleChoice(agenda, invocation);
        agenda.Executions.Suspend(invocation);

        Assert.False(invocation.IsComplete);
        Assert.Throws<InvalidOperationException>(() => agenda.Advance(owner, occurrence));
        Assert.True(agenda.IsOutstanding(owner, occurrence));
        Assert.Equal(2, agenda.Count);
    }

    [Fact]
    public void AContinuationCanResumeOnlyOnceAndOnlyInItsGame()
    {
        var agenda = AgendaWithOwner();
        var invocation = agenda.Executions.Begin(agenda.Occurrence!, new(7, AbilityType.Response, 0));
        ScheduleChoice(agenda, invocation);
        agenda.Executions.Suspend(invocation);
        var foreign = AgendaWithOwner();

        Assert.Throws<InvalidOperationException>(() => foreign.Executions.Resume(invocation));
        agenda.Executions.Resume(invocation);
        Assert.Throws<InvalidOperationException>(() => agenda.Executions.Resume(invocation));
        agenda.Executions.Complete(invocation);
        Assert.True(invocation.IsComplete);
        Assert.Equal(1, foreign.Count);
        Assert.Equal(Stage.Interrupts, foreign.Stage);
    }

    [Fact]
    public void RemovingAnUnansweredChoiceCannotCompleteItsParent()
    {
        var agenda = AgendaWithOwner();
        var invocation = agenda.Executions.Begin(agenda.Occurrence!, new(7, AbilityType.Response, 0));
        ScheduleChoice(agenda, invocation);
        agenda.Executions.Suspend(invocation);
        var child = agenda.Current!.Value;
        var occurrence = agenda.Occurrence!;
        Assert.True(agenda.Advance(child, occurrence));

        Assert.Throws<InvalidOperationException>(() => agenda.Advance(child, occurrence));
        Assert.False(invocation.IsComplete);
        Assert.True(agenda.IsOutstanding(child, occurrence));
    }

    [Fact]
    public void AnOwnerlessParentCannotOvertakeItsChildContinuation()
    {
        var agenda = new Agenda();
        var occurrence = new Occurrence(0, [Steps.CardPlayed]);
        var parent = agenda.Executions.BeginBatch(occurrence);
        var child = agenda.Executions.Begin(occurrence, new(7, AbilityType.Interrupt, 0));
        ScheduleChoice(agenda, child, occurrence);
        child.Yield([]);
        ScheduleChoice(agenda, parent, occurrence);
        parent.Yield([]);

        Assert.Throws<InvalidOperationException>(() => agenda.Executions.Resume(parent));
        agenda.Executions.Resume(child);
        child.Finish([]);
        Assert.False(parent.IsComplete);
        agenda.Advance();
        agenda.Advance();
        agenda.Executions.Resume(parent);
        parent.Finish([]);
        Assert.True(parent.IsComplete);
        Assert.True(child.IsComplete);
    }

    [Marvel.Tests.Rule("rr:activation.8")]
    [Fact]
    public void DeferredActivationObligationDoesNotPreventItsEnclosingActivationFromFinishing()
    {
        // rr:activation.8: an activation initiated during another resolves
        // after the current activation has finished. Execution completion still
        // waits for that deferred obligation; readiness to advance is distinct.
        var agenda = AgendaWithOwner();
        var owner = agenda.Current!.Value;
        var occurrence = agenda.Occurrence!;
        var parent = agenda.Executions.BeginBatch(occurrence);
        var child = agenda.Executions.Begin(occurrence, new(7, AbilityType.Interrupt, 0));
        ScheduleChoice(agenda, child);
        agenda.Executions.DeferActivation(child);
        child.Yield([]);
        parent.Finish([]);

        Assert.False(parent.IsComplete);
        Assert.Equal(owner, agenda.Current!.Value);
        agenda.Advance();
        agenda.Advance();
        agenda.Advance();
        agenda.Executions.Resume(child);
        child.Finish([]);
        Assert.True(parent.IsComplete);
    }

    [Fact]
    public void CancellationCannotDiscardAnUnconsumedContinuation()
    {
        var agenda = AgendaWithOwner();
        var invocation = agenda.Executions.Begin(agenda.Occurrence!, new(7, AbilityType.Response, 0));
        ScheduleChoice(agenda, invocation);
        invocation.Yield([]);
        var choice = agenda.Current!.Value;
        var occurrence = agenda.Occurrence!;

        Assert.Throws<InvalidOperationException>(() => agenda.Cancel(choice, occurrence));
        Assert.True(agenda.IsOutstanding(choice, occurrence));
        Assert.False(invocation.IsComplete);
        agenda.Executions.Resume(invocation);
        invocation.Finish([]);
        Assert.True(invocation.IsComplete);
    }

    private static Agenda AgendaWithOwner()
    {
        var agenda = new Agenda();
        agenda.Add(new PhaseStep(new LifecycleOperation(LifecycleKind.CardPlayed), 1, 1));
        return agenda;
    }

    private static void ScheduleChoice(Agenda agenda, AbilityExecution invocation,
        Occurrence? occurrence = null) =>
        agenda.ThenContinuation(new PhaseStep(
            new AbilityContinuationAgendaOperation(AbilityContinuationStage.Choose)
            { Continuation = new Continuation { Execution = invocation } }, 1, 1), occurrence ?? agenda.Occurrence!);

    private sealed record Continuation : CardAbilityContinuation;
}
