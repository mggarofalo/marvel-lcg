using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Owns the consequences of a nested card effect, separately from its trigger.</summary>
public static class CardEffectWindows
{
    /// <summary>Begin recording an immediate effect before its response window is walked.</summary>
    public static void Begin(Agenda agenda, int source, int player)
    {
        var parent = agenda.Current
            ?? throw new InvalidOperationException("a nested effect requires an agenda owner");
        int id = agenda.nextPlayerActionOccurrence--;
        var occurrence = new Occurrence(id, [], Subject: source, Player: player);
        var step = new PhaseStep(new CompletedCardEffectOperation(), parent.Round,
            parent.Number, Subject: source, Seat: player) { OccurrenceId = id };
        // rr:triggering-condition.2 shares conditions from one occurrence. A
        // response's effect is a new occurrence, not another result of its trigger.
        // Immediate damage/defeat procedures supply their own interrupt points;
        // this frame retains their resulting conditions for the response window.
        agenda.items.Insert(0, agenda.Frame(step, Stage.Responses, occurrence));
        agenda.scheduled = 0;
    }
}
