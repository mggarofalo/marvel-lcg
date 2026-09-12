using static Marvel.Cards.Run.AbilityRuntimeQueries;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Projects ability costs into area mutation state.</summary>
internal static class AbilityAreaCostProjection
{
    internal static void TraceCost(this AbilityAreaProjection projection, AbilityCost payment, AreaProjectionState state)
    {
        if (IsHandCost(payment))
        {
            projection.couldDiscard |= projection.queried.Contains(DeckType.HandsArea)
                || projection.queried.Contains(DeckType.DiscardPile);
            return;
        }

        switch (payment)
        {
            case AbilityCost.Sequence sequence:
                projection.TraceSequenceCost(sequence, state);
                return;
            case AbilityCost.Damage damage:
                projection.TraceDamageCost(damage, state);
                return;
            case AbilityCost.Heal heal:
                projection.TraceHealCost(heal, state);
                return;
            case AbilityCost.Discard discard:
                projection.TraceDiscardCost(discard, state);
                return;
            case AbilityCost.RemoveCounters counters:
                projection.TraceCounterCost(counters, state);
                return;
            case AbilityCost.Exhaust:
            case AbilityCost.ExhaustChosen:
                return;
            default:
                throw new InvalidOperationException(
                    "Unknown compiled cost in area projection");
        }
    }

    private static bool IsHandCost(AbilityCost payment) =>
        payment is AbilityCost.DiscardFromHand or AbilityCost.Spend
            or AbilityCost.SpendEnergy;

    private static Card? CostTarget(this AbilityAreaProjection projection, AbilityCostCard binding, AreaProjectionState state)
    {
        var card = Named(binding switch
        {
            AbilityCostCard.Source => AbilityCardBinding.This,
            AbilityCostCard.Identity => AbilityCardBinding.You,
            _ => throw new InvalidOperationException("Unknown compiled cost binding"),
        }, projection.context);
        return card is not null && !state.Departed.Contains(card.ObjectId)
            ? card : null;
    }

    private static void TraceSequenceCost(
        this AbilityAreaProjection projection, AbilityCost.Sequence sequence, AreaProjectionState state)
    {
        var damageFirst = sequence.Costs
            .Where(step => step is AbilityCost.Damage { MustTakeAll: true });
        var spendingSecond = sequence.Costs.OfType<AbilityCost.Spend>();
        var remaining = sequence.Costs.Where(step => step is not
            (AbilityCost.Spend or AbilityCost.Damage { MustTakeAll: true }));
        foreach (var step in damageFirst.Concat(spendingSecond).Concat(remaining))
        {
            projection.TraceCost(step, state);
        }
    }

    private static void TraceDamageCost(this AbilityAreaProjection projection, AbilityCost.Damage damage, AreaProjectionState state)
    {
        if (projection.CostTarget(damage.Card, state) is not { } target)
        {
            return;
        }
        long amount = damage.MustTakeAll
            ? damage.Amount
            : ModifiedAbilityDamage(damage.Amount, projection.context);
        projection.DealProjected(state, target, amount, 1);
    }

    private static void TraceHealCost(this AbilityAreaProjection projection, AbilityCost.Heal heal, AreaProjectionState state)
    {
        if (projection.CostTarget(heal.Card, state) is { } target)
        {
            state.Damage[target.ObjectId] = Math.Max(
                0, state.DamageOf(target) - heal.Amount);
        }
    }

    private static void TraceDiscardCost(
        this AbilityAreaProjection projection, AbilityCost.Discard discard, AreaProjectionState state)
    {
        if (projection.CostTarget(discard.Card, state) is not { } target)
        {
            return;
        }
        projection.couldDiscard |= projection.queried.Contains(target.Area.Type)
            || projection.DiscardTreeChangesArea(target);
        projection.MarkDiscardedTree(state, target);
    }

    private static void TraceCounterCost(
        this AbilityAreaProjection projection, AbilityCost.RemoveCounters counters, AreaProjectionState state)
    {
        if (projection.CostTarget(counters.Card, state) is not { } holder)
        {
            return;
        }
        long removed = checked(
            projection.removedCounters.GetValueOrDefault(holder.ObjectId) + counters.Count);
        projection.removedCounters[holder.ObjectId] = removed;
        if (!projection.UsesExhausted(holder, removed))
        {
            return;
        }
        projection.couldDiscard |= projection.queried.Contains(holder.Area.Type)
            || (Keywords.Has(projection.context.World, holder, "victory", projection.context.World.Facts)
                ? projection.HostedCardsChangeArea(state, holder.ObjectId)
                : projection.DiscardTreeChangesArea(holder));
        projection.MarkDiscardedTree(state, holder);
    }

    private static bool UsesExhausted(this AbilityAreaProjection projection, Card holder, long removed) =>
        CounterCount(holder, "allPurpose") == removed
        && !Characteristics.IsLost(projection.context.World, holder, "uses")
        && AbilityProgramQueries.CounterPool(
            projection.context.World, projection.context.Program, holder)?.Uses == true;
}
