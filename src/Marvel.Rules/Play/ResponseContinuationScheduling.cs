using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Schedules enclosing work after a completed character power's response window.</summary>
public static class ResponseContinuationScheduling
{
    /// <summary>Retains ordinary child prerequisites while waiting for an exact live owner window.</summary>
    public static void AfterResponseWindow(this Agenda agenda, AbilityExecution execution,
        PhaseStep owner, Occurrence occurrence, PhaseStep continuation, PhaseStep? prerequisite = null)
    {
        agenda.Executions.RequireActive(execution);
        int at = RequireOwner(agenda, owner, occurrence);
        RequireExecution(execution, owner, occurrence, continuation);
        (PhaseStep Step, Stage Stage, Occurrence? Occurrence)? child = prerequisite is null
            ? null : agenda.Frame(prerequisite.Value, Stage.Interrupts, null);
        var frame = agenda.Frame(continuation with { Plan = true }, Stage.Apply, execution.Occurrence);
        var permit = frame.Step.ContinuationPermit
            ?? throw new InvalidOperationException("The response continuation has no execution permit");
        frame.Step = frame.Step with { ContinuationPermit = new(execution, frame.Step.FrameId,
            [.. permit.Dependencies.Append(owner.FrameId)
                .Concat(child is { } pending ? [pending.Step.FrameId] : Array.Empty<long>()).Distinct()]) };
        execution.ResponseBoundary = new(owner, occurrence, frame.Step.FrameId, child?.Step.FrameId);
        if (child is { } work) agenda.items.Insert(at + 1, work);
        agenda.items.Insert(at + (child is null ? 1 : 2), frame);
        agenda.scheduled = 0;
    }

    private static void RequireExecution(AbilityExecution execution, PhaseStep owner,
        Occurrence occurrence, PhaseStep continuation)
    {
        if (ReferenceEquals(execution.Occurrence, occurrence) || owner.ExecutionOwner != execution
            || execution.ResponseBoundary is not null || ContinuationExecution.Of(continuation) != execution)
            throw new InvalidOperationException("The response continuation requires its exact execution");
    }

    private static int RequireOwner(Agenda agenda, PhaseStep owner, Occurrence occurrence)
    {
        int at = agenda.items.FindIndex(item => item.Step.SameFrameAs(owner)
            && ReferenceEquals(item.Occurrence, occurrence));
        if (at < 0 || agenda.items[at].Stage == Stage.Interrupts
            || owner.FrameGame != agenda.Executions.Identity || owner.Plan)
            throw new InvalidOperationException("The response continuation requires its exact live owner");
        return at;
    }
}
