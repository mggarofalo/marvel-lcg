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
    internal DamageRequest Request => request;
    internal Defeated? Defeated { get; private set; }
    internal bool IsLethal(World world, ICardFacts facts) => IsCurrent && prepared is { Landed: true }
        && DamagePlacement.Health(world, facts, request.Target) - request.Target.Damage <= 0;
    internal void RecordDefeat(World world) => Defeated = Defeat.Record(world, request.Target,
        request.By, request.Verb, request.RecordDefeatOn);
    internal bool Depart(World world, ICardFacts facts, List<GameEvent> events) =>
        Defeated is not null && IsCurrent && Defeat.FinalizeCharacter(world, facts, request.Target, request.Trigger, events);
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
        AttackDamageAccounting.RecordPlacement(world, request.ActivationId, prepared!);
        if (prepared!.Landed) request.RecordDefeatOn?.Also(Steps.DamageDealt);
    }
    internal bool Settle(World world, ICardFacts facts, AbilityExecution execution, List<GameEvent> events)
    {
        if (Result is null)
            world.Agenda.Executions.RunBatch(execution.Occurrence, entry =>
            {
                var resolution = new DamageResolution(world, facts, request, entry, targetIncarnation: incarnation);
                Result = resolution.Result;
                resolution.Settle(prepared!, events,
                    Defeated is null ? Damage.Outcome.NotDefeated : Damage.Outcome.Defeated);
                return entry.Report(events);
            });
        return Result!.IsComplete;
    }
}
