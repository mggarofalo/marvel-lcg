using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Positions suspended ability work inside its owning occurrence.</summary>
public static class AgendaContinuationScheduling
{
    /// <summary>Schedule a suspended ability continuation inside its occurrence.</summary>
    /// <remarks>
    /// A choice is an implementation suspension point, not a second game
    /// occurrence. The continuation therefore asks during <paramref name="occurrence"/>
    /// and opens no windows of its own; the owning occurrence reaches its one
    /// response window only after the continuation finishes.
    /// </remarks>
    public static void ThenContinuation(this Agenda agenda, PhaseStep step, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        agenda.BeginScheduling();
        agenda.scheduled += 1;
        var frame = agenda.Frame(
            step with { Plan = true, OccurrenceId = occurrence.Id }, Stage.Apply, occurrence);
        agenda.items.Insert(Math.Min(agenda.scheduled, agenda.items.Count), frame);
        agenda.scheduledFrames.Add(frame.Step.FrameId);
    }

    /// <summary>Put every child and then a continuation before its exact owner.</summary>
    /// <remarks>
    /// Several nested procedure frames may share the owner's occurrence. The
    /// frame identity distinguishes the original owner from those children, so the
    /// continuation remains behind every child even when more than one of them
    /// has already moved ahead of the owner.
    /// </remarks>
    public static void BeforeOwnerAfterContinuations(
        this Agenda agenda, Occurrence occurrence, PhaseStep owner, PhaseStep continuation)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        int ownerAt = agenda.items.FindIndex(item =>
            ReferenceEquals(item.Occurrence, occurrence)
            && item.Step.SameFrameAs(owner));
        if (ownerAt < 0)
        {
            throw new InvalidOperationException(
                $"the '{owner.What}' occurrence owner is not on the agenda");
        }

        var children = agenda.items
            .Where((item, index) =>
                index != ownerAt && ReferenceEquals(item.Occurrence, occurrence))
            .ToList();
        agenda.items.RemoveAll(item =>
            ReferenceEquals(item.Occurrence, occurrence)
            && !item.Step.SameFrameAs(owner));
        ownerAt = agenda.items.FindIndex(item =>
            ReferenceEquals(item.Occurrence, occurrence)
            && item.Step.SameFrameAs(owner));
        agenda.items.InsertRange(ownerAt, children);
        agenda.items.Insert(
            ownerAt + children.Count,
            agenda.Frame(continuation with
            {
                Plan = true,
                OccurrenceId = occurrence.Id,
            }, Stage.Apply, occurrence));
        agenda.scheduled = 0;
    }

    /// <summary>Insert an ability continuation immediately before its exact owner.</summary>
    /// <remarks>
    /// A nested rules procedure has already moved its own frames ahead of the
    /// owner. Inserting at the owner's current index leaves those frames first
    /// while keeping pre-existing enclosing continuations behind the ability
    /// that must finish before they resume.
    /// </remarks>
    public static void ContinueBeforeOwner(
        this Agenda agenda, Occurrence occurrence, PhaseStep owner, PhaseStep continuation,
        Occurrence? continuationOccurrence = null)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        int ownerAt = agenda.items.FindIndex(item => item.Step.SameFrameAs(owner)
            && ReferenceEquals(item.Occurrence, occurrence));
        if (ownerAt < 0)
        {
            throw new InvalidOperationException("the continuation owner is not on the agenda");
        }

        agenda.items.Insert(
            ownerAt,
            agenda.Frame(continuation with
            {
                Plan = true,
                OccurrenceId = (continuationOccurrence ?? occurrence).Id,
            }, Stage.Apply, continuationOccurrence ?? occurrence));
        agenda.scheduled = 0;
    }
}
