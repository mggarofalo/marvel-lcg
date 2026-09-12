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
        switch (stage)
        {
            case Stage.Interrupts:
                agenda.items[0] = (step, Stage.Apply, occurrence);
                return true;

            case Stage.Apply:
                agenda.items[0] = (step, Stage.Responses, occurrence);
                return true;

            default:
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
        switch (stage)
        {
            case Stage.Interrupts:
                agenda.items[at] = (step, Stage.Apply, found);
                return true;
            case Stage.Apply:
                agenda.items[at] = (step, Stage.Responses, found);
                return true;
            default:
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
    /// The step value is engine agenda data and supplies that missing address.
    /// </remarks>
    public static bool Advance(this Agenda agenda, PhaseStep owner, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        int at = agenda.items.FindIndex(item => item.Step.Equals(owner)
            && ReferenceEquals(item.Occurrence, occurrence));
        if (at < 0)
        {
            throw new InvalidOperationException("the agenda item is not outstanding");
        }

        var (step, stage, found) = agenda.items[at];
        switch (stage)
        {
            case Stage.Interrupts:
                agenda.items[at] = (step, Stage.Apply, found);
                return true;
            case Stage.Apply:
                agenda.items[at] = (step, Stage.Responses, found);
                return true;
            default:
                agenda.items.RemoveAt(at);
                if (at == 0)
                {
                    agenda.scheduled = 0;
                }
                return false;
        }
    }

    /// <summary>Remove a replaced occurrence and both of its remaining windows.</summary>
    public static void Cancel(this Agenda agenda, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        int at = agenda.items.FindIndex(item => ReferenceEquals(item.Occurrence, occurrence));
        if (at < 0)
        {
            throw new InvalidOperationException("the occurrence is not on the agenda");
        }

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
            && item.Step.Character == target);
        if (at >= 0)
        {
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
        agenda.items.Clear();
        agenda.scheduled = 0;
    }
}
