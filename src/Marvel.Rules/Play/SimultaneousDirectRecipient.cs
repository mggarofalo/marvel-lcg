using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Retains one direct-damage recipient through the simultaneous placement barrier.</summary>
internal sealed class SimultaneousDirectRecipient(DamageRequest request)
{
    private readonly int incarnation = request.Target.Incarnation;
    private DamageReplacement? replacement;
    private Damage.PlacedDamage? prepared;
    internal DamageResult? Result { get; private set; }
    private bool IsCurrent => request.Target.Incarnation == incarnation
        && DeckTypes.IsInPlay(request.Target.Area.Type);
    internal bool Replace(World world, List<GameEvent> events)
    {
        if (!IsCurrent) return true;
        AttackDamageAccounting.ValidateBoostPlacement(world, request.ActivationId);
        replacement ??= DamagePlacement.Replace(world, request, events);
        return replacement.IsComplete;
    }
    internal void Prepare(World world, ICardFacts facts, List<GameEvent> events)
    {
        AttackDamageAccounting.ValidateBoostPlacement(world, request.ActivationId);
        prepared = IsCurrent && replacement is { IsComplete: true }
            ? DamagePlacement.PrepareAfterReplacement(world, facts,
                request with { Amount = replacement.ResolvedAmount }, events)
            : new Damage.PlacedDamage(request.Target, 0, 0);
    }
    internal void Place(World world, ICardFacts facts, List<GameEvent> events)
    {
        if (IsCurrent) DamagePlacement.ApplyPlaced(world, facts, prepared!, request.Trigger, request.Verb, events);
    }
    internal bool Settle(World world, ICardFacts facts, AbilityExecution execution, List<GameEvent> events)
    {
        if (Result is null)
            world.Agenda.Executions.RunBatch(execution.Occurrence, entry =>
            {
                var resolution = new DamageResolution(world, facts, request, entry, targetIncarnation: incarnation);
                Result = resolution.Result;
                resolution.Settle(prepared!, events);
                return entry.Report(events);
            });
        return Result!.IsComplete;
    }
}
