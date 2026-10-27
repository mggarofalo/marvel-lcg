using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class EnclosingAbilityContinuationsTests
{
    [Fact]
    public void LineageExcludesUnrelatedWorkAndKeepsNearestParentFirst()
    {
        var agenda = new Agenda();
        var occurrence = new Occurrence(1, [Steps.CardPlayed]);
        var unrelated = agenda.Executions.BeginBatch(occurrence);
        Schedule(agenda, unrelated, occurrence);
        unrelated.Yield([]);
        var outer = agenda.Executions.BeginBatch(occurrence);
        var parent = agenda.Executions.BeginBatch(occurrence);
        var child = agenda.Executions.BeginBatch(occurrence);
        Schedule(agenda, child, occurrence);
        child.Yield([]);
        var parentState = Schedule(agenda, parent, occurrence);
        parent.Yield([]);
        var outerState = Schedule(agenda, outer, occurrence);
        outer.Yield([]);
        var current = agenda.Current!.Value;
        var result = EnclosingAbilityContinuations.For(agenda, current);
        Assert.Equal(new CardAbilityContinuation[] { parentState, outerState }, result);
        Assert.DoesNotContain(result, item => item.Execution == unrelated || item.Execution == child);
        Assert.Throws<InvalidOperationException>(() => EnclosingAbilityContinuations.For(new Agenda(), current));
        Assert.Equal(current, agenda.Current);
        Assert.False(child.IsComplete);
        agenda.Executions.Resume(child);
        child.Finish([]);
        Assert.Throws<InvalidOperationException>(() => EnclosingAbilityContinuations.For(agenda, current));
        Assert.Equal(2, result.Count);
    }

    private static Continuation Schedule(Agenda agenda, AbilityExecution execution, Occurrence occurrence)
    {
        var state = new Continuation { Execution = execution };
        agenda.ThenContinuation(new PhaseStep(
            new AbilityContinuationAgendaOperation(AbilityContinuationStage.Choose)
            { Continuation = state }, 1, 1), occurrence);
        return state;
    }

    private sealed record Continuation : CardAbilityContinuation;
}
