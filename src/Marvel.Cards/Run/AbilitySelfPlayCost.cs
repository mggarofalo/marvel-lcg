using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Reads one physical candidate's play-cost text without activating it in hand.</summary>
internal static class AbilitySelfPlayCost
{
    internal static long Reduction(World world, AbilityProgram program, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentOutOfRangeException.ThrowIfNegative(player);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(player, world.Players);
        if (!world.Cards.Contains(card) || card.Area.Type != DeckType.HandsArea
            || card.Area.PlayArea != PlayArea.Of(player) || card.Owner != player) return 0;
        long reduction = 0;
        // This query's explicit player is the candidate's future controller, not the active turn seat.
        foreach (var ability in AbilityProgramQueries.ActiveOn(world, program, card))
            if (ability.Trigger.Timing == AbilityType.Constant)
                reduction = checked(reduction + ReductionFor(world, ability.Effect, player));
        return reduction;
    }

    private static long ReductionFor(World world, AbilityEffect effect, int player) => effect switch
    {
        // Printed 06011: "for each minion engaged with you."
        AbilityEffect.ReduceSelfCostPerEngagedMinion discount => checked(discount.Amount * world.Areas
                    .Where(area => area.Type == DeckType.EngagedEnemiesArea && area.PlayArea == PlayArea.Of(player))
                    .SelectMany(area => area.Cards)
                    .LongCount(minion => EffectiveCards.Kind(minion, world.Facts) == CardKind.Minion)),
        // Printed 08002: "for each Preparation card you control." No card-kind restriction.
        AbilityEffect.ReduceSelfCostPerControlledTrait discount => checked(discount.Amount * world.Cards
            .LongCount(controlled => DeckTypes.IsInPlay(controlled.Area.Type)
                && CardControl.ControllerOf(world, controlled) == player
                && Traits.Has(world, controlled, discount.Trait, world.Facts))),
        _ => 0,
    };
}
