using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Reads suspended interpreter work belonging to the current decision's ancestors.</summary>
public static class EnclosingAbilityContinuations
{
    /// <summary>Nearest ancestors first; excludes unrelated work and rejects stale decision frames.</summary>
    public static IReadOnlyList<CardAbilityContinuation> For(Agenda agenda, PhaseStep current)
    {
        ArgumentNullException.ThrowIfNull(agenda);
        agenda.Executions.RequireContinuation(current);
        var result = new List<CardAbilityContinuation>();
        for (var parent = ContinuationExecution.Of(current)?.Parent; parent is not null; parent = parent.Parent)
        {
            foreach (var item in agenda.items)
                if (item.Step.Operation is AbilityContinuationAgendaOperation { Continuation: { } state }
                    && ReferenceEquals(state.Execution, parent))
                    result.Add(state);
        }
        return result.ToArray();
    }
}
