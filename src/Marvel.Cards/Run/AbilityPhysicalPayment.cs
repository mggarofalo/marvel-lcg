using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Commits physical costs against the currently bound card incarnation.</summary>
internal sealed class AbilityPhysicalPayment(World world, Card source)
{
    internal static void RequireReady(Card card)
    {
        // rr:initiating-abilities.3: departure cannot prevent "a required cost from being paid".
        // rr:initiating-abilities.step.5: if costs cannot be paid, "abort this process".
        if (!card.Ready)
            throw new RulesNotImplementedException($"card {card.ObjectId} is exhausted and cannot pay its required exhaust cost");
    }

    internal void Discard(Card target, string trigger, List<GameEvent> events)
    {
        bool removableArea = DeckTypes.IsInPlay(target.Area.Type)
            || target.Area.Type is DeckType.BoostingArea
                or DeckType.ProcessingArea or DeckType.RevealingArea;
        if (!removableArea || !Marvel.Rules.Play.Discard.EffectCanRemove(world, world.Facts, source, target))
            throw new RulesNotImplementedException($"card {target.ObjectId} cannot pay its required discard cost");
        Marvel.Rules.Play.Discard.CardFromEffect(world, world.Facts, source, target, trigger, events);
    }

    internal void RemoveCounters(
        Card target, string counter, long count, ICardCounterPools pools,
        string trigger, List<GameEvent> events)
    {
        AbilityCardOperations.RemoveCounters(
            world, pools, target, counter, count, trigger, events);
    }

    internal long Heal(Card target, long amount, string trigger, List<GameEvent> events)
    {
        // rr:initiating-abilities.step.5: if required costs cannot be paid, "abort this process".
        if (target.Damage < amount)
            throw new RulesNotImplementedException(
                $"card {target.ObjectId} cannot heal the required {amount} damage paying this cost");
        return DamageRecovery.Heal(world, world.Facts, target, amount, trigger, "Heal", events);
    }

    internal DamageCostReceipt? Damage(
        Card target, long writtenAmount, AbilityExecution owner, string trigger, List<GameEvent> events)
    {
        // Costs are not attacks. Event modifiers remain live because an earlier
        // payment can remove the card granting a modifier.
        long amount = AbilityAmounts.SaturatingSum(writtenAmount,
            [AbilityEventModifiers.Amount(world, source, "eventDamage")]);
        return DamagePlacement.BeginCost(world, world.Facts,
            new DamageRequest(source, target, amount, trigger, "Deal_Damage"), owner, events);
    }

}
