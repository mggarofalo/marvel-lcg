using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Traces health targets together through ordered cost damage and healing.</summary>
internal sealed class AbilityHealingCostProjection(
    World world, Card source, int player, AbilityProgram program, IResourceCardAbilities resources,
    Card target)
{
    private readonly Dictionary<int, AbilityHealingTarget> targets = [];
    private string? pendingCounterLifecycle;
    private AbilityQueryProjection projection = new(ImmutableDictionary<int, CardInstanceState>.Empty);

    internal bool CanPay(AbilityCost cost, Card? generator = null, AbilityCost? generatorCost = null)
    {
        var steps = AbilityCostOrder.Flatten(cost).ToArray();
        foreach (var step in steps.Where(AbilityCostOrder.IsTaking))
            if (!Apply(step, source)) return false;
        if (generator is not null)
        {
            foreach (var step in AbilityCostOrder.Flatten(generatorCost))
                if (!Apply(step, generator)) return false;
            // A resource ability settles defeat before the outer arrow cost.
            if (State(target).Damage >= DamagePlacement.Health(world, world.Facts, target))
                throw new RulesNotImplementedException("projecting lethal resource damage before healing is not implemented");
        }
        foreach (var step in steps.Where(step => !AbilityCostOrder.IsTaking(step)))
            if (!Apply(step, source)) return false;
        return true;
    }

    private AbilityHealingTarget State(Card card)
    {
        if (!targets.TryGetValue(card.ObjectId, out var state))
            targets.Add(card.ObjectId, state = new AbilityHealingTarget(world, card));
        return state;
    }

    private bool Apply(AbilityCost step, Card payer)
    {
        if (!ApplyQuantities(step, payer)) return false;
        return ApplyHealth(step, payer);
    }

    private bool ApplyQuantities(AbilityCost step, Card payer)
    {
        if (step is AbilityCost.Exhaust exhaust
            && AbilityPaymentRules.CostTarget(world, payer, player, exhaust.Card) is { } exhausted)
            projection = projection.WithReady(exhausted, false);
        if (step is AbilityCost.ExhaustTitled named
            && NamedAbilityCost.Target(world, player, named.Title) is { } titled)
            projection = projection.WithReady(titled, false);
        if (step is AbilityCost.RemoveCounters removal)
            return RemoveCounters(removal, payer);
        return true;
    }

    private bool ApplyHealth(AbilityCost step, Card payer)
    {
        if (step is AbilityCost.Damage or AbilityCost.Heal && pendingCounterLifecycle is { } reason)
            throw new RulesNotImplementedException(reason);
        if (step is AbilityCost.Damage hit
            && AbilityPaymentRules.CostTarget(world, payer, player, hit.Card) is { } damaged)
            return ApplyDamage(hit, payer, damaged);
        if (step is not AbilityCost.Heal heal
            || AbilityPaymentRules.CostTarget(world, payer, player, heal.Card) is not { } healed)
            return true;
        var state = State(healed);
        if (state.Damage < heal.Amount) return false;
        state.Damage -= heal.Amount;
        projection = projection.WithDamage(healed, state.Damage);
        return true;
    }

    private bool RemoveCounters(AbilityCost.RemoveCounters cost, Card payer)
    {
        var card = AbilityPaymentRules.CostTarget(world, payer, player, cost.Card);
        if (card is null) return false;
        var quantities = projection.Quantities(card);
        string? key = AbilityCostSelection.CounterKeyForRemoval(
            quantities, cost.Counter, cost.Count, card.FaceId);
        if (key is null) return false;
        projection = projection.WithCounters(card, key, quantities.Tokens[key] - cost.Count);
        if (GreenGoblinTransition.HandlesCounterRemoval(card, cost.Counter)
            && projection.Quantities(card).Tokens[key] == 0)
            pendingCounterLifecycle = "projecting native counter transitions before health costs is not implemented";
        else if (AbilityCardOperations.CounterRemovalDeparts(world,
                     AbilityProgramQueries.CounterPool(world, program, card), card, projection.Quantities(card)))
            pendingCounterLifecycle = "projecting the last Uses counter departure before health costs is not implemented";
        return true;
    }

    private bool ApplyDamage(AbilityCost.Damage cost, Card payer, Card damaged)
    {
        if (!AbilityProgramQueries.CanTakeDamage(world, program, damaged, payer, projection)) return !cost.MustTakeAll;
        long amount = cost.MustTakeAll ? cost.Amount : AbilityAmounts.SaturatingSum(cost.Amount,
            [AbilityEventModifiers.Amount(world, payer, "eventDamage")]);
        var replacement = new AbilityDamageProjection(program, resources) { Projection = projection }
            .PreviewDamageReplacement(world, damaged, payer, amount);
        if (replacement.Result is not RuleProjection<long>.Known known)
            throw new RulesNotImplementedException("projecting an unknown damage replacement before healing is not implemented");
        var state = State(damaged);
        long taken = state.TakeDamage(known.Value);
        projection = projection.WithDamage(damaged, state.Damage);
        return !cost.MustTakeAll || taken == cost.Amount;
    }
}
