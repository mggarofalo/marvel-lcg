using Marvel.Rules.Events;
using static Marvel.View.EventSubjectDescriptions;

namespace Marvel.View;

/// <summary>Copies explicit instruction and phase context from authorized engine facts.</summary>
internal static class ActivityContextProjection
{
    internal static EventActivityDescriptor With(EventActivityDescriptor projected,
        GameEvent happened, WorldDescriptor world) => projected with
    {
        Batch = happened is ActivityCompleted completed ? completed.Batch : happened.ActivityBatch,
        Round = happened.Round,
        Phase = Phase(happened),
        PhaseBoundary = happened is PhaseStarted,
        PhaseStep = happened is ActivityCompleted { PhaseStep: not null },
        Participants = Participants(projected, happened, world),
    };

    private static string Phase(GameEvent happened) => happened switch
    {
        PhaseStarted phase => phase.Phase,
        ActivityCompleted { PhaseStep: not null } => "Player",
        _ => string.Empty,
    };

    private static ActivityParticipantDescriptor[] Participants(EventActivityDescriptor projected,
        GameEvent happened, WorldDescriptor world)
    {
        if (happened is not ActivityCompleted activity) return [];
        return activity.Participants.Select(participant => new ActivityParticipantDescriptor(
            participant.Player, CompletedActivityPresentation.ParticipantName(participant.Player, world), participant.Count,
            projected.Subjects.Where(subject => participant.Cards.Contains(subject.Card)).ToArray())).ToArray();
    }
}
