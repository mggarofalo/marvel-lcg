using static Marvel.Cards.Run.AbilityProjectedDefeat;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Settles projected damage, prevention and defeated hosted cards.</summary>
internal sealed class AbilityAreaDamageProjection(AbilityAreaProjection projection)
{
    private AbilityAdmissionContext context => projection.context;

    private bool DiscardedByDamage(AreaProjectionState state, Card target) =>
        state.DamageOf(target) >= state.HealthOf(context, target)
        && DefeatTreeChangesArea(target, projection.queried, context);

    private bool DefeatedHostsCardsChangeArea(
        AreaProjectionState state, int host) =>
        projection.ProjectedHostedCards(state, host).Any(card =>
        {
            bool movesToVictory = EffectiveCards.Kind(card, context.World.Facts) is
                    CardKind.Attachment or CardKind.Upgrade
                && Keywords.Has(
                    context.World, card, "victory", context.World.Facts);
            return movesToVictory
                ? projection.ProjectedHostedCards(state, card.ObjectId)
                    .Any(child => projection.DiscardTreeChangesArea(child))
                : projection.DiscardTreeChangesArea(card);
        });

    internal void DealProjected(
        AreaProjectionState state, Card target, long amount,
        long repetitions)
    {
        var villains = new AbilityAreaVillainProjection(projection);
        target = villains.ProjectedDamageTarget(state, target);
        if (!CanDealProjectedDamage(state, target, amount, repetitions))
        {
            return;
        }

        long prevented = Math.Min(state.ToughOf(context, target), repetitions);
        state.Tough[target.ObjectId] = state.ToughOf(context, target) - prevented;
        long dealt = AbilityAmounts.SaturatingMultiply(amount, repetitions - prevented);
        state.Damage[target.ObjectId] = AbilityAmounts.SaturatingSum(
            state.DamageOf(target), [dealt]);
        projection.couldDiscard |= DiscardedByDamage(state, target);
        if (state.DamageOf(target) < state.HealthOf(context, target))
        {
            return;
        }

        if (CardKinds.IsVillain(EffectiveCards.Kind(target, context.World.Facts)))
        {
            villains.DefeatProjectedVillain(state, target);
        }
        else if (villains.RootLeavesOnDefeat(target))
        {
            projection.couldDiscard |= DefeatedHostsCardsChangeArea(state, target.ObjectId);
            projection.MarkDiscardedTree(state, target);
        }
    }

    private bool CanDealProjectedDamage(
        AreaProjectionState state, Card target, long amount, long repetitions) =>
        amount > 0
        && repetitions > 0
        && !state.Departed.Contains(target.ObjectId)
        && AbilityProgramQueries.CanTakeDamage(
            context.World, context.Program, target, context.Source);
}
