using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Executes damage replacement, placement and defeat as one owned procedure.</summary>
internal sealed class DamageResolution(World world, ICardFacts facts, DamageRequest request, AbilityExecution execution, AbilityExecution? completion = null, int? targetIncarnation = null)
{
    private readonly int incarnation = targetIncarnation ?? request.Target.Incarnation;
    private readonly DamageEventProvenance provenance = new(execution);
    private DamageReplacement? replacement;
    private bool placed;
    private DirectDamagePrevention? prevention;
    private bool preventionSuspended;
    private Damage.PlacedDamage? placement;
    private EffectCompletion? excessThreat;
    private readonly int priorDefeats = request.RecordDefeatOn?.Defeats.Count ?? 0;
    internal DamageResult Result { get; } = new(completion ?? execution) { Target = request.Target.ObjectId, TargetIncarnation = targetIncarnation ?? request.Target.Incarnation };
    internal AbilityExecution Execution => execution;

    internal void Continue(List<GameEvent> events)
    {
        int start = events.Count;
        ContinueSegment(events);
        if (preventionSuspended) provenance.Associate(events, start);
    }

    private void ContinueSegment(List<GameEvent> events)
    {
        world.Agenda.Executions.RequireActive(execution);
        if (placed)
        {
            Complete(request.RecordDefeatOn?.Defeats.Skip(priorDefeats).Any(defeat =>
                defeat.Card == request.Target.ObjectId && defeat.Incarnation == incarnation) == true
                ? Damage.Outcome.Defeated : Damage.Outcome.NotDefeated, events);
            return;
        }
        AttackDamageAccounting.ValidateBoostPlacement(world, request.ActivationId);
        replacement ??= DamagePlacement.Replace(world, request, events);
        if (!replacement.IsComplete) { Suspend(events); return; }
        if (request.Target.Incarnation != incarnation || !DeckTypes.IsInPlay(request.Target.Area.Type))
        {
            Result.Place(new(request.Target, 0, 0));
            Result.Complete(Damage.Outcome.NotDefeated);
            execution.Finish(events);
            return;
        }
        AttackDamageAccounting.ValidateBoostPlacement(world, request.ActivationId);
        var damage = PrepareDamage(events);
        if (damage is null) { Suspend(events); return; }
        DamagePlacement.ApplyPlaced(world, facts, damage, request.Trigger, request.Verb, events);
        Settle(damage, events);
    }

    private Damage.PlacedDamage? PrepareDamage(List<GameEvent> events)
    {
        prevention ??= DirectDamagePrevention.Begin(world, request, replacement!,
            DamagePlacement.PrepareAfterReplacement(world, facts,
                request with { Amount = replacement!.ResolvedAmount }, events));
        if (prevention.IsComplete) return prevention.Placement;
        preventionSuspended = true;
        return null;
    }

    internal void Settle(Damage.PlacedDamage damage, List<GameEvent> events, Damage.Outcome? settledOutcome = null)
    {
        world.Agenda.Executions.RequireActive(execution);
        Result.Place(damage);
        if (settledOutcome is null) AttackDamageAccounting.RecordPlacement(world, request.ActivationId, damage);
        placement = damage;
        if (damage.Landed) request.RecordDefeatOn?.Also(Steps.DamageDealt);
        placed = true;
        var outcome = settledOutcome ?? DamagePlacement.FinishPlaced(world, facts, request.Source, damage,
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
