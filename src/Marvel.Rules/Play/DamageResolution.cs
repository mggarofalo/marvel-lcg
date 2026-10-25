using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Executes damage replacement, placement and defeat as one owned procedure.</summary>
internal sealed class DamageResolution(World world, ICardFacts facts, DamageRequest request, AbilityExecution execution, AbilityExecution? completion = null)
{
    private readonly int incarnation = request.Target.Incarnation;
    private DamageReplacement? replacement;
    private bool placed;
    private Damage.PlacedDamage? placement;
    private EffectCompletion? excessThreat;
    private readonly int priorDefeats = request.RecordDefeatOn?.Defeats.Count ?? 0;
    internal DamageResult Result { get; } = new(completion ?? execution) { Target = request.Target.ObjectId, TargetIncarnation = request.Target.Incarnation };
    internal AbilityExecution Execution => execution;

    internal void Continue(List<GameEvent> events)
    {
        world.Agenda.Executions.RequireActive(execution);
        if (placed)
        {
            Complete(request.RecordDefeatOn?.Defeats.Skip(priorDefeats).Any(defeat =>
                defeat.Card == request.Target.ObjectId && defeat.Incarnation == incarnation) == true
                ? Damage.Outcome.Defeated : Damage.Outcome.NotDefeated, events);
            return;
        }
        replacement ??= DamagePlacement.Replace(world, request, events);
        if (!replacement.IsComplete) { Suspend(events); return; }
        if (request.Target.Incarnation != incarnation || !DeckTypes.IsInPlay(request.Target.Area.Type))
        {
            Result.Place(new(request.Target, 0, 0));
            Result.Complete(Damage.Outcome.NotDefeated);
            execution.Finish(events);
            return;
        }
        var damage = DamagePlacement.PrepareAfterReplacement(world, facts, request.Source, request.Target,
            replacement.ResolvedAmount, request.Trigger, events);
        DamagePlacement.ApplyPlaced(world, facts, damage, request.Trigger, request.Verb, events);
        Settle(damage, events);
    }

    internal void Settle(Damage.PlacedDamage damage, List<GameEvent> events)
    {
        world.Agenda.Executions.RequireActive(execution);
        Result.Place(damage);
        placement = damage;
        if (damage.Landed) request.RecordDefeatOn?.Also(Steps.DamageDealt);
        placed = true;
        var outcome = DamagePlacement.FinishPlaced(world, facts, request.Source, damage,
            request.Trigger, request.Verb, events, request.By, request.RecordDefeatOn);
        if (outcome == Damage.Outcome.Suspended) { Suspend(events); return; }
        Complete(outcome, events);
    }

    private void Complete(Damage.Outcome outcome, List<GameEvent> events)
    {
        long excess = Math.Max(0, placement!.Taken - placement.RemainingHealth);
        if (excessThreat is null && placement.ExcessThreat is { } route
            && outcome == Damage.Outcome.Defeated && excess > 0)
        {
            excessThreat = route.Schedule(world, excess, request.Trigger);
            Suspend(events);
            return;
        }
        if (excessThreat is { IsComplete: false }) { Suspend(events); return; }
        Result.Complete(outcome);
        execution.Finish(events);
    }

    private void Suspend(List<GameEvent> events)
    {
        var step = new PhaseStep(new DamageResolutionOperation(this), world.Agenda.Current?.Round ?? 0, 5, Plan: true);
        world.Agenda.ThenContinuation(step, execution.Occurrence);
        execution.Yield(events);
    }

    internal static void Resume(World world, PhaseStep step, List<GameEvent> events)
    {
        var resolution = step.Require<DamageResolutionOperation>().Resolution;
        world.Agenda.Executions.RequireContinuation(step);
        world.Agenda.Executions.Resume(resolution.Execution);
        resolution.Continue(events);
    }
}
