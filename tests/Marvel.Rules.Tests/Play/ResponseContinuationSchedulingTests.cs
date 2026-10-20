using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class ResponseContinuationSchedulingTests
{
    [Fact]
    public void ContinuationWaitsForExactOwnerResponsesAndResumesOnlyOnce()
    {
        // Frame identity and checked scheduling are engine representation choices.
        var (agenda, execution, owner, occurrence) = ApplyingOwner();
        agenda.AfterResponseWindow(execution, owner, occurrence, Continuation(execution));
        execution.Yield([]);
        Assert.Throws<InvalidOperationException>(() => agenda.Executions.Resume(execution));
        Assert.True(agenda.Advance(owner, occurrence));
        Assert.Equal(Stage.Responses, agenda.Stage);
        Assert.Throws<InvalidOperationException>(() => agenda.Executions.Resume(execution));
        Assert.False(agenda.Advance(owner, occurrence));
        agenda.Executions.Resume(execution);
        Assert.Throws<InvalidOperationException>(() => agenda.Executions.Resume(execution));
        execution.Finish([]);
        Assert.True(execution.IsComplete);
    }

    [Theory]
    [InlineData("unstarted")]
    [InlineData("stale")]
    [InlineData("foreign")]
    [InlineData("wrong-owner")]
    public void InvalidOwnerCannotRegisterAnyContinuation(string invalid)
    {
        var (agenda, execution, owner, occurrence) = ApplyingOwner();
        if (invalid == "unstarted")
        {
            agenda.Add(new(new LifecycleOperation(LifecycleKind.CardPlayed), 2, 1));
            agenda.Advance(owner, occurrence);
            agenda.Advance(owner, occurrence);
            owner = agenda.Current!.Value;
            occurrence = agenda.Occurrence!;
        }
        else if (invalid == "stale")
        {
            agenda.Advance(owner, occurrence);
            agenda.Advance(owner, occurrence);
        }
        else if (invalid == "foreign")
        {
            var foreign = ApplyingOwner();
            owner = foreign.Owner;
            occurrence = foreign.Occurrence;
        }
        else
        {
            var child = agenda.Executions.BeginBatch(occurrence);
            agenda.Add(new(new LifecycleOperation(LifecycleKind.CardPlayed), 2, 1));
            child.Finish([]);
            agenda.Advance(owner, occurrence);
            agenda.Advance(owner, occurrence);
            owner = agenda.Current!.Value;
            occurrence = agenda.Occurrence!;
            agenda.Advance(owner, occurrence);
        }
        int before = agenda.Count;
        Assert.Throws<InvalidOperationException>(() =>
            agenda.AfterResponseWindow(execution, owner, occurrence, Continuation(execution)));
        Assert.Equal(before, agenda.Count);
        Assert.False(execution.IsComplete);
    }

    private static (Agenda Agenda, AbilityExecution Execution, PhaseStep Owner, Occurrence Occurrence)
        ApplyingOwner()
    {
        var agenda = new Agenda();
        var execution = agenda.Executions.BeginBatch(new(1, ["OuterAbility"]));
        agenda.Add(new(new LifecycleOperation(LifecycleKind.CardPlayed), 1, 1));
        var owner = agenda.Current!.Value;
        var occurrence = agenda.Occurrence!;
        agenda.Advance();
        return (agenda, execution, owner, occurrence);
    }

    private static PhaseStep Continuation(AbilityExecution execution) => new(
        new AbilityContinuationAgendaOperation(AbilityContinuationStage.Resume)
        { Continuation = new SavedContinuation { Execution = execution } }, 1, 1);

    private sealed record SavedContinuation : CardAbilityContinuation;
}
