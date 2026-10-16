using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Owns an otherwise standalone damage procedure and its actual response window.</summary>
internal static class RootDamageExecution
{
    internal static AbilityExecutionResult Run(World world, Card actor, Card target,
        Func<Occurrence, AbilityExecution, AbilityExecutionResult> apply)
    {
        var agenda = world.Agenda;
        int id = agenda.nextPlayerActionOccurrence--;
        var cause = Occurrence.ForAttack(id, [], world, world.Facts,
            actor.ObjectId, target.ObjectId, target.Owner);
        // Entry ownership is distinct from the effect's triggering occurrence:
        // its pending response frame must not block that frame's own window.
        return agenda.Executions.RunBatch(new Occurrence(id, []), owner =>
        {
            var applied = apply(cause, owner);
            var response = new PhaseStep(new CompletedCardEffectOperation(), 0, 1,
                Subject: actor.ObjectId, Seat: target.Owner)
            { OccurrenceId = id, CompletionPrerequisite = applied.Invocation };
            agenda.items.Add(agenda.Frame(response, Stage.Responses, cause));
            return owner.Finish(applied.Events);
        });
    }
}
