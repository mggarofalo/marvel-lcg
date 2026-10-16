using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Fixes and pays damage without opening its defeat window before sibling costs are paid.</summary>
internal sealed class DamageCostPlacement(World world, ICardFacts facts, DamageRequest request,
    DamageCostReceipt receipt)
{
    private readonly int incarnation = request.Target.Incarnation;
    private DamageReplacement? replacement;
    internal AbilityExecution Execution => receipt.PlacementExecution;

    internal void Continue(List<GameEvent> events)
    {
        world.Agenda.Executions.RequireActive(Execution);
        replacement ??= DamagePlacement.Replace(world, request, events);
        if (!replacement.IsComplete)
        {
            world.Agenda.ThenContinuation(new PhaseStep(new DamageCostPlacementOperation(this),
                world.Agenda.Current?.Round ?? 0, 5, Plan: true), Execution.Occurrence);
            Execution.Yield(events);
            return;
        }
        var damage = request.Target.Incarnation == incarnation && DeckTypes.IsInPlay(request.Target.Area.Type)
            ? DamagePlacement.PrepareAfterReplacement(world, facts, request.Source, request.Target,
                replacement.ResolvedAmount, request.Trigger, events)
            : new Damage.PlacedDamage(request.Target, 0, 0);
        DamagePlacement.ApplyPlaced(world, facts, damage, request.Trigger, request.Verb, events);
        receipt.Place(damage);
        Execution.Finish(events);
    }

    internal static void Resume(World world, PhaseStep step, List<GameEvent> events)
    {
        var placement = step.Require<DamageCostPlacementOperation>().Placement;
        world.Agenda.Executions.RequireContinuation(step);
        world.Agenda.Executions.Resume(placement.Execution);
        placement.Continue(events);
    }
}
