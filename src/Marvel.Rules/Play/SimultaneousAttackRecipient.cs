using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Retains one selected incarnation through a simultaneous damage barrier.</summary>
internal sealed class SimultaneousAttackRecipient(AttackDamageRequest request)
{
    private readonly int incarnation = request.Target.Incarnation;
    private DamageReplacement? replacement;
    private bool pierced;
    private PreparedAttackDamage? prepared;
    internal AttackDamageResult? Result { get; private set; }

    private bool IsCurrent => request.Target.Incarnation == incarnation
        && DeckTypes.IsInPlay(request.Target.Area.Type);

    internal bool Replace(World world, AbilityExecution execution, List<GameEvent> events)
    {
        if (!IsCurrent) return true;
        if (!pierced)
        {
            pierced = true;
            if (AttackKeywords.Has(world, request.Attacker, request.Source, Keywords.Piercing))
                DamageAttacks.DiscardPiercedTough(world, request.Target, request.Amount, request.Trigger, events);
        }
        replacement ??= DamagePlacement.Replace(world,
            new(request.Source, request.Target, request.Amount, request.Trigger, request.Verb,
                LabeledAbilities.IdentityPlayer(world, request.Attacker))
            { RecordDefeatOn = execution.Occurrence, FromAttack = true }, events);
        return replacement.IsComplete;
    }

    internal void Prepare(World world, ICardFacts facts, List<GameEvent> events)
    {
        var target = request.Target;
        long remaining = Math.Max(0, DamagePlacement.Health(world, facts, target) - target.Damage);
        var kind = EffectiveCards.Kind(target, facts);
        int controller = kind == CardKind.Ally ? target.Area.PlayArea.Player : -1;
        var placement = IsCurrent && replacement is { IsComplete: true }
            ? DamagePlacement.PrepareAfterReplacement(world, facts,
                new DamageRequest(request.Source, target, replacement.ResolvedAmount, request.Trigger, request.Verb), events)
            : new Damage.PlacedDamage(target, 0, 0);
        prepared = new(placement, remaining, kind, controller, incarnation, AttackDamagePolicy.Capture(world, request));
    }

    internal void Place(World world, ICardFacts facts, List<GameEvent> events)
    {
        if (IsCurrent)
            DamagePlacement.ApplyPlaced(world, facts, prepared!.Placement, request.Trigger, request.Verb, events);
    }

    internal bool Settle(World world, ICardFacts facts, AbilityExecution execution, List<GameEvent> events)
    {
        Result ??= DamageAttacks.Begin(world, facts,
            request with
            {
                Prepared = prepared! with { Policy = prepared.Policy with { Retaliate = false } },
                Origin = execution.Occurrence,
            }, events, spillOnly: false);
        return Result.IsComplete;
    }

    internal void Retaliate(World world, ICardFacts facts, List<GameEvent> events)
    {
        if (IsCurrent && prepared!.Policy.Retaliate)
            DamageAttacks.Retaliate(world, facts, request.Target, request.Attacker, request.Trigger, events);
    }
}
