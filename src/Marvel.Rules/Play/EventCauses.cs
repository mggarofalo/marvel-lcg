using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Reads existing causal ownership without evaluating rules or advancing the agenda.</summary>
internal static class EventCauses
{
    internal static EventCause? From(World world)
    {
        if (world.Activation is { Attacking: false } activation)
            return Create(world, activation.Id, EventCauseKind.Scheme, activation.Enemy,
                world.Seats[activation.Player].IdentityCard.ObjectId);
        foreach (var (step, _, occurrence) in world.Agenda.items)
            if (step.Operation is CharacterThwartOperation { Thwart: { } thwart })
                return Create(world, occurrence?.Id ?? step.OccurrenceId ?? Moment.Id(step.Round, step.Number, step.Index),
                    EventCauseKind.Thwart, thwart.Thwarter, thwart.Scheme);
        return null;
    }

    private static EventCause Create(World world, int occurrence, EventCauseKind kind, int source, int target) =>
        new(occurrence, kind, source, target)
        {
            SourceTitle = AttackCompletion.SubjectTitle(world, world.Facts, source),
            TargetTitle = target >= 0 ? AttackCompletion.SubjectTitle(world, world.Facts, target) : string.Empty,
        };
}
