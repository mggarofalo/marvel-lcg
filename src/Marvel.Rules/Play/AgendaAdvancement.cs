using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Advances, cancels, and abandons agenda work.</summary>
public static class AgendaAdvancement
{

    /// <summary>Move the current step on to its next part.</summary>
    /// <returns>False when the step is finished and has been taken off the list.</returns>
    public static bool Advance(this Agenda agenda)
    {
        var (step, stage, occurrence) = agenda.items[0];
        agenda.Executions.RequireAdvance(step);
        switch (stage)
        {
            case Stage.Interrupts:
                agenda.items[0] = (step, Stage.Apply, occurrence);
                return true;

            case Stage.Apply:
            case Stage.AwaitingCompletion:
                agenda.items[0] = (step, Stage.Responses, occurrence);
                return true;

            default:
                agenda.Executions.Removed(step);
                agenda.items.RemoveAt(0);
                agenda.scheduled = 0;
                return false;
        }
    }

    /// <summary>Advance the item that owns <paramref name="occurrence"/>.</summary>
    /// <remarks>
    /// An applying card ability may put a nested occurrence in front of itself.
    /// Advancing by identity keeps the outer item moving to its response window
    /// without accidentally skipping the newly inserted interrupt window.
    /// </remarks>
    public static bool Advance(this Agenda agenda, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        int at = agenda.items.FindIndex(item => ReferenceEquals(item.Occurrence, occurrence));
        if (at < 0)
        {
            throw new InvalidOperationException("the occurrence is not on the agenda");
        }

        var (step, stage, found) = agenda.items[at];
        agenda.Executions.RequireAdvance(step);
        switch (stage)
        {
            case Stage.Interrupts:
                agenda.items[at] = (step, Stage.Apply, found);
                return true;
            case Stage.Apply:
            case Stage.AwaitingCompletion:
                agenda.items[at] = (step, Stage.Responses, found);
                return true;
            default:
                agenda.Executions.Removed(step);
                agenda.items.RemoveAt(at);
                if (at == 0)
                {
                    agenda.scheduled = 0;
                }
                return false;
        }
    }

    /// <summary>Advance one exact agenda item that owns an occurrence.</summary>
    /// <remarks>
    /// A procedure continuation deliberately shares its parent's occurrence.
    /// Once such a continuation has moved in front of the parent, occurrence
    /// identity alone is no longer enough to identify which item just applied.
    /// The scheduler-assigned frame identity supplies that missing address even
    /// when the procedure payload changes.
    /// </remarks>
    public static bool Advance(this Agenda agenda, PhaseStep owner, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        int at = agenda.items.FindIndex(item => item.Step.SameFrameAs(owner)
            && ReferenceEquals(item.Occurrence, occurrence));
        if (at < 0)
        {
            throw new InvalidOperationException("the agenda item is not outstanding");
        }

        var (step, stage, found) = agenda.items[at];
        agenda.Executions.RequireAdvance(step);
        switch (stage)
        {
            case Stage.Interrupts:
                agenda.items[at] = (step, Stage.Apply, found);
                return true;
            case Stage.Apply:
            case Stage.AwaitingCompletion:
                agenda.items[at] = (step, Stage.Responses, found);
                return true;
            default:
                agenda.Executions.Removed(step);
                agenda.items.RemoveAt(at);
                if (at == 0)
                {
                    agenda.scheduled = 0;
                }
                return false;
        }
    }

    /// <summary>Record an applied body, waiting explicitly for its immediate owned work.</summary>
    internal static void Applied(this Agenda agenda, PhaseStep owner, Occurrence occurrence)
    {
        if (!agenda.Executions.OwnsUnfinishedWork(owner))
        {
            agenda.Advance(owner, occurrence);
            return;
        }
        int at = agenda.items.FindIndex(item => item.Step.SameFrameAs(owner)
            && ReferenceEquals(item.Occurrence, occurrence));
        if (at < 0 || agenda.items[at].Stage != Stage.Apply)
            throw new InvalidOperationException("only an applying frame can await its child work");
        var item = agenda.items[at];
        agenda.items[at] = (item.Step, Stage.AwaitingCompletion, item.Occurrence);
    }

    /// <summary>Remove a replaced occurrence and both of its remaining windows.</summary>
    public static void Cancel(this Agenda agenda, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        var owner = agenda.items.Single(item => !item.Step.Plan
            && ReferenceEquals(item.Occurrence, occurrence)).Step;
        agenda.Cancel(owner, occurrence);
    }

    /// <summary>Cancel an exact occurrence owner without discarding suspended ability cleanup.</summary>
    public static void Cancel(this Agenda agenda, PhaseStep owner, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        int at = agenda.items.FindIndex(item => item.Step.SameFrameAs(owner)
            && ReferenceEquals(item.Occurrence, occurrence));
        if (at < 0)
        {
            throw new InvalidOperationException("the occurrence is not on the agenda");
        }

        if (agenda.items[at].Step.Operation is LifecycleOperation { EventInitiation: { } initiation })
            initiation.Cancel();
        if (agenda.items[at].Step.Operation is IdentityFormChangeOperation form)
            form.Completion.Complete(0, 0);
        if (agenda.items[at].Step.Operation is ThreatPlacementOperation threat)
            threat.Completion.Complete(threat.Part, 0);
        if (agenda.items[at].Step.Operation is EncounterEntryOperation entry && !entry.Completion.IsComplete)
            entry.Completion.Complete(0, 0);
        agenda.Executions.Removed(agenda.items[at].Step, cancelled: true);
        agenda.items.RemoveAt(at);
        if (at == 0)
        {
            agenda.scheduled = 0;
        }
    }

    /// <summary>Remove the consequential-damage step for an aborted ally power.</summary>
    /// <remarks>
    /// The rulebook does not define an agenda operation. This is the engine's
    /// representation of <c>rr:consequential-damage.2</c>: when the target left
    /// before the basic power applied, the already-exhausted ally takes no
    /// consequential damage.
    /// </remarks>
    public static void CancelConsequentialDamage(this Agenda agenda, int ally, int target, bool attack)
    {
        string what = attack
            ? Steps.AllyConsequentialDamage
            : Steps.AllyThwartConsequentialDamage;
        int at = agenda.items.FindIndex(item =>
            item.Step.What == what
            && item.Step.Subject == ally
            && item.Step.Operation is ConsequentialDamageOperation { Character: var character }
            && character == target);
        if (at >= 0)
        {
            if (agenda.items[at].Step.Operation is EncounterEntryOperation entry && !entry.Completion.IsComplete)
            entry.Completion.Complete(0, 0);
        agenda.Executions.Removed(agenda.items[at].Step, cancelled: true);
            agenda.items.RemoveAt(at);
            if (at <= agenda.scheduled)
            {
                agenda.scheduled = Math.Max(0, agenda.scheduled - 1);
            }
        }
    }

    /// <summary>
    /// Abandon everything outstanding.
    /// </summary>
    /// <remarks>
    /// For the end of the game. <c>rr:winning-the-game</c> and
    /// <c>rr:main-scheme-main-scheme-deck.2.1</c> both end it outright, and the
    /// rest of the villain phase does not happen.
    /// </remarks>
    public static void Abandon(this Agenda agenda)
    {
        agenda.Executions.Abandon();
        agenda.items.Clear();
        agenda.scheduled = 0;
    }
}
