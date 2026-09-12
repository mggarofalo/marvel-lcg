using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Schedules and completes nested enemy activations.</summary>
public static class AgendaActivationScheduling
{

    /// <summary>Schedule one complete enemy activation and return its stable id.</summary>
    /// <remarks>
    /// The completion sentinel is present before the activation starts, so an
    /// activation initiated during one of its substeps can be placed after the
    /// whole activation rather than merely after that substep. This is
    /// <c>rr:activation.8</c>. The id is allocated monotonically; it is an
    /// engine wire choice rather than a Rules Reference value.
    /// </remarks>
    public static int ThenActivation(this Agenda agenda, PhaseStep step)
    {
        if (!agenda.IsActivation(step))
        {
            throw new ArgumentException("the step is not an enemy activation", nameof(step));
        }

        int id = agenda.nextActivationId++;
        var root = step with { ActivationId = id };
        var completion = agenda.Completion(root);
        var pair = new[]
        {
            (root, Stage.Interrupts, root.ScheduledOccurrence),
            (completion, Stage.Interrupts, completion.ScheduledOccurrence),
        };

        if (agenda.items.Count == 0)
        {
            agenda.items.AddRange(pair);
            agenda.scheduled = 0;
            return id;
        }

        int currentActivation = agenda.Current?.ActivationId ?? -1;
        if (currentActivation >= 0)
        {
            int at = agenda.CompletionIndex(currentActivation) + 1;
            if (agenda.queuedAfterActivation.TryGetValue(currentActivation, out var queued))
            {
                foreach (int queuedId in queued)
                {
                    at = Math.Max(at, agenda.CompletionIndex(queuedId) + 1);
                }
            }
            else
            {
                agenda.queuedAfterActivation[currentActivation] = queued = [];
            }

            agenda.items.InsertRange(at, pair);
            queued.Add(id);
            return id;
        }

        agenda.scheduled += pair.Length;
        agenda.items.InsertRange(Math.Min(agenda.scheduled - pair.Length + 1, agenda.items.Count), pair);
        return id;
    }

    /// <summary>
    /// Schedule a step to be taken <i>before</i> the current one happens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:interrupt.1</c>: an interrupt "resolves <b>before</b> the
    /// triggering condition". For an interrupt whose effect is itself an
    /// activation that is not enough on its own, because
    /// <c>rr:activation.8</c> would otherwise put the new activation after —
    /// "an activation initiated during another resolves after the current
    /// activation has finished resolving". Speed Demon prints the exception
    /// as a reminder: "<i>(Resolve Speed Demon's attack first.)</i>"
    /// </para>
    /// <para>
    /// The step it goes in front of keeps the stage it had reached, so the
    /// interrupt window that was open re-opens when the agenda comes back to
    /// it. That is <c>rr:interrupt.5</c> and not an accident: using an
    /// interrupt "gives each player another opportunity" to use one.
    /// </para>
    /// </remarks>
    /// <param name="agenda">The agenda receiving the work.</param>
    /// <param name="step">What to do first.</param>
    public static void Now(this Agenda agenda, PhaseStep step)
    {
        if (agenda.IsActivation(step))
        {
            _ = agenda.NowActivation(step);
            return;
        }

        agenda.Now([step]);
    }

    /// <summary>Schedule several steps now without reversing their order.</summary>
    public static void Now(this Agenda agenda, IReadOnlyList<PhaseStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        agenda.items.InsertRange(
            0,
            steps.Select(step => (step, Stage.Interrupts, step.ScheduledOccurrence)));

        // The inserted step is where `Then` now counts from, and it has
        // agenda.scheduled nothing of its own yet.
        agenda.scheduled = 0;
    }

    /// <summary>Insert a plan immediately before the item that owns an occurrence.</summary>
    /// <remarks>
    /// A nested occurrence may have been inserted in front of the occurrence
    /// that caused it. Defeat uses this boundary for damage step 8: every
    /// nested step-7 effect remains in front, while leaving play remains before
    /// the original occurrence resumes its response window at step 9.
    /// </remarks>
    public static void Before(this Agenda agenda, Occurrence occurrence, PhaseStep step)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        int at = agenda.items.FindIndex(item => ReferenceEquals(item.Occurrence, occurrence));
        if (at < 0)
        {
            throw new InvalidOperationException("the occurrence is not on the agenda");
        }

        agenda.items.Insert(at, (step, Stage.Interrupts, step.ScheduledOccurrence));
    }

    internal static void AddActivation(this Agenda agenda, PhaseStep step)
    {
        int id = agenda.nextActivationId++;
        var root = step with { ActivationId = id };
        var completion = agenda.Completion(root);
        agenda.items.Add((root, Stage.Interrupts, root.ScheduledOccurrence));
        agenda.items.Add((completion, Stage.Interrupts, completion.ScheduledOccurrence));
    }

    /// <summary>Schedule an immediate activation and return its stable id.</summary>
    public static int NowActivation(this Agenda agenda, PhaseStep step)
    {
        if (!agenda.IsActivation(step))
        {
            throw new ArgumentException("the step is not an enemy activation", nameof(step));
        }
        int id = agenda.nextActivationId++;
        var root = step with { ActivationId = id };
        var completion = agenda.Completion(root);
        agenda.items.InsertRange(0,
        [
            (root, Stage.Interrupts, root.ScheduledOccurrence),
            (completion, Stage.Interrupts, completion.ScheduledOccurrence),
        ]);
        agenda.scheduled = 0;
        return id;
    }

    /// <summary>Place one persisted ability continuation after all named activations.</summary>
    public static void AfterActivations(this Agenda agenda, IReadOnlyList<int> activationIds, PhaseStep continuation)
    {
        ArgumentNullException.ThrowIfNull(activationIds);
        if (activationIds.Count == 0)
        {
            throw new ArgumentException("at least one activation is required", nameof(activationIds));
        }
        int at = activationIds.Max(agenda.CompletionIndex) + 1;
        agenda.items.Insert(at, (
            continuation with { Plan = true },
            Stage.Apply,
            continuation.AbilityOccurrence));
    }

    /// <summary>Find a continuation waiting for one activation without interpreting its payload.</summary>
    public static PhaseStep? ActivationWait(this Agenda agenda, int activationId)
    {
        int at = agenda.items.FindIndex(item =>
            item.Step.What == Steps.ResumeAbility
            && item.Step.AbilityActivationIds?.Contains(activationId) == true);
        if (at < 0)
        {
            return null;
        }
        return agenda.items[at].Step;
    }

    /// <summary>Replace one activation wait as opaque agenda.scheduled work.</summary>
    public static void ReplaceActivationWait(this Agenda agenda, int activationId, PhaseStep replacement)
    {
        int at = agenda.ActivationWaitIndex(activationId);
        if (at < 0) throw new InvalidOperationException("the activation wait is not on the agenda");
        var item = agenda.items[at];
        agenda.items[at] = (replacement, item.Stage, item.Occurrence);
    }

    /// <summary>Take one activation wait as opaque agenda.scheduled work.</summary>
    public static PhaseStep TakeActivationWait(this Agenda agenda, int activationId)
    {
        int at = agenda.ActivationWaitIndex(activationId);
        if (at < 0) throw new InvalidOperationException("the activation wait is not on the agenda");
        var step = agenda.items[at].Step;
        agenda.items.RemoveAt(at);
        return step;
    }

    internal static int ActivationWaitIndex(this Agenda agenda, int activationId) => agenda.items.FindIndex(item =>
        item.Step.What == Steps.ResumeAbility
        && item.Step.AbilityActivationIds?.Contains(activationId) == true);

    internal static int CompletionIndex(this Agenda agenda, int activationId)
    {
        int found = agenda.items.FindIndex(item =>
            item.Step.ActivationId == activationId
            && item.Step.What is Steps.CompleteAttackActivation
                or Steps.CompleteSchemeActivation);
        return found >= 0
            ? found
            : throw new InvalidOperationException(
                $"activation {activationId} has no completion sentinel");
    }

    /// <summary>Remove the unfinished steps of an activation that ended early.</summary>
    public static void EndActivationEarly(this Agenda agenda, int activationId, bool preserveCurrentOccurrence = true)
    {
        if (activationId < 0)
        {
            return;
        }

        // Keep the current occurrence so its response window can resolve, and
        // keep the completion sentinel so the effect that initiated the
        // activation still receives its result and can resume.
        int first = preserveCurrentOccurrence ? 1 : 0;
        for (int index = agenda.items.Count - 1; index >= first; index--)
        {
            var item = agenda.items[index];
            if (item.Step.ActivationId == activationId
                && item.Step.What is not (Steps.CompleteAttackActivation
                    or Steps.CompleteSchemeActivation))
            {
                agenda.items.RemoveAt(index);
            }
        }
    }

    internal static bool IsActivation(this Agenda agenda, PhaseStep step) =>
        step.What is Steps.Attack or Steps.Scheme && step.ActivationId < 0;

    internal static PhaseStep Completion(this Agenda agenda, PhaseStep root) => new(
        root.What == Steps.Attack
            ? Steps.CompleteAttackActivation
            : Steps.CompleteSchemeActivation,
        root.Round,
        root.Number,
        Index: root.Index,
        Subject: root.Subject,
        Seat: root.Seat,
        Plan: true,
        ActivationId: root.ActivationId);
}
