using System.Collections.Immutable;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Publishes one completed Preparation ability through the execution ledger.</summary>
public static class PreparationAbilityCompletion
{
    /// <summary>Capture before costs; later continuations retain the same invocation receipt.</summary>
    public static void Track(World world, AbilityExecution execution, Card source, int player)
    {
        world.Agenda.Executions.RequireActive(execution);
        if (execution.IsBatch || execution.Preparation is not null
            || execution.Ability.Type == AbilityType.Constant) return;
        var traits = world.Facts.Traits(source.FaceId).ToImmutableArray();
        if (!traits.Contains("PREPARATION", StringComparer.Ordinal)) return;
        execution.Preparation = new(source.ObjectId, source.FaceId, source.Incarnation,
            CardControl.ControllerOf(world, source), player, traits, execution.Ability)
            { SourceFacts = OccurrenceCard.Capture(source, world.Facts) with
                { Controller = CardControl.ControllerOf(world, source) } };
    }

    internal static void Complete(Agenda agenda, AbilityExecution execution, PhaseStep? removing)
    {
        if (execution.Preparation is not { } receipt
            || execution.Occurrence.Resolution.StatusOf(receipt.Ability) != ResolutionStatus.Resolved) return;
        var step = new PhaseStep(new PreparationAbilityResolvedOperation(receipt),
            agenda.Current?.Round ?? 0, 0, Subject: receipt.Source, Seat: receipt.Player)
            { OccurrenceId = agenda.nextPlayerActionOccurrence-- };
        // This condition describes completed resolution, so there is no imminent
        // occurrence to interrupt. rr:response.3 permits either response order.
        var occurrence = new Occurrence(step.OccurrenceId.Value, [step.What], receipt.Source, receipt.Player,
            Actor: receipt.Source, ActorFacts: receipt.SourceFacts! with { PreparationAbility = receipt });
        var frame = agenda.Frame(step, Stage.Responses, occurrence);
        int after = removing is { } removed
            ? agenda.items.FindIndex(item => item.Step.SameFrameAs(removed)) + 1 : 0;
        agenda.items.Insert(after, frame);
    }
}
