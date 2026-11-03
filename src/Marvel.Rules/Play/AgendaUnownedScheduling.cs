namespace Marvel.Rules.Play;

/// <summary>Schedules work that follows an ability without belonging to it.</summary>
internal static class AgendaUnownedScheduling
{
    /// <summary>Schedule a step as <see cref="Agenda.Then"/> does, owned by no ability invocation.</summary>
    /// <remarks>
    /// A triggered ability of another card is not part of the instruction whose result
    /// triggered it, so the instruction's completion does not wait for it. Which work an
    /// invocation owns is an engine choice.
    /// </remarks>
    internal static void ThenUnowned(this Agenda agenda, PhaseStep step)
    {
        agenda.BeginScheduling();
        agenda.scheduled += 1;
        var frame = agenda.Unowned(step with { ExecutionOwner = null, CompletionPrerequisite = null, ContinuationPermit = null });
        agenda.items.Insert(Math.Min(agenda.scheduled, agenda.items.Count), (frame, Stage.Interrupts, frame.ScheduledOccurrence));
        agenda.scheduledFrames.Add(frame.FrameId);
    }
}
