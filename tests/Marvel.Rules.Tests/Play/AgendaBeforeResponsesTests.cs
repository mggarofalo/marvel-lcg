using Marvel.Rules.Play;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class AgendaBeforeResponsesTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ProcedureChildrenMoveBeforeResponsesOnlyWithinTheirExecution(bool sameExecution)
    {
        // Execution ownership is an engine choice. A procedure inherits its
        // interpreter's execution; a nested invocation retains its own ordering.
        var agenda = new Agenda();
        var entry = agenda.Executions.BeginBatch(new Occurrence(1, [Steps.CardPlayed]));
        agenda.Then(new PhaseStep(new LifecycleOperation(LifecycleKind.CardPlayed), 1, 1, Subject: 4));
        var owner = agenda.Current!.Value;
        var occurrence = agenda.Occurrence!;
        var nested = sameExecution ? null : agenda.Executions.BeginBatch(occurrence);
        agenda.Then(new PhaseStep(new LifecycleOperation(LifecycleKind.CardPlayed), 1, 1, Subject: 5));

        agenda.BeforeResponses(owner, occurrence);

        int[] expected = sameExecution ? [5, 4] : [4, 5];
        Assert.Equal(expected, agenda.Outstanding.Select(step => step.Subject));
        Assert.True(agenda.IsOutstanding(owner, occurrence));
        Assert.False(entry.IsComplete);
        if (nested is not null) Assert.False(nested.IsComplete);
    }
}
