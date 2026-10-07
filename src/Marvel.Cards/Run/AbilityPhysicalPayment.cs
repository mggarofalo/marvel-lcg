using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Commits physical costs against the currently bound card incarnation.</summary>
internal sealed class AbilityPhysicalPayment(World world, Card source)
{
    internal void Discard(Card? target, string trigger, List<GameEvent> events)
    {
        if (target is null) return;
        bool removableArea = DeckTypes.IsInPlay(target.Area.Type)
            || target.Area.Type is DeckType.BoostingArea
                or DeckType.ProcessingArea or DeckType.RevealingArea;
        if (removableArea && Marvel.Rules.Play.Discard.EffectCanRemove(world, world.Facts, source, target))
            Marvel.Rules.Play.Discard.CardFromEffect(world, world.Facts, source, target, trigger, events);
    }

    internal void RemoveCounters(
        Card? target, string counter, long count, ICardCounterPools pools,
        string trigger, List<GameEvent> events)
    {
        var holder = target
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' cannot find the card paying its counter cost");
        AbilityCardOperations.RemoveCounters(
            world, pools, holder, counter, count, trigger, events);
    }

    internal long Heal(Card? target, long amount, string trigger, List<GameEvent> events) =>
        target is not null
            ? DamageRecovery.Heal(
                world, world.Facts, target, amount, trigger, "Heal", events)
            : 0;

    internal bool Damage(
        Card? target, long writtenAmount, string trigger, List<GameEvent> events)
    {
        // Costs are not attacks. Event modifiers remain live because an earlier
        // payment can remove the card granting a modifier.
        long amount = AbilityAmounts.SaturatingSum(writtenAmount,
            [AbilityEventModifiers.Amount(world, source, "eventDamage")]);
        return target is not null
            && DamagePlacement.DealOutcome(
                world, world.Facts, source, target, amount, trigger,
                "Deal_Damage", events) == Marvel.Rules.Play.Damage.Outcome.Suspended;
    }

}
