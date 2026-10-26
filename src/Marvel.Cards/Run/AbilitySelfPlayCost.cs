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
        // Printed 06011: "Reduce the cost to play Hercules by 1 for each minion engaged with you."
        // This query's explicit player is the candidate's future controller, not the active turn seat.
        foreach (var ability in AbilityProgramQueries.ActiveOn(world, program, card))
            if (ability.Trigger.Timing == AbilityType.Constant
                && ability.Effect is AbilityEffect.ReduceSelfCostPerEngagedMinion discount)
                reduction = checked(reduction + discount.Amount * world.Areas
                    .Where(area => area.Type == DeckType.EngagedEnemiesArea && area.PlayArea == PlayArea.Of(player))
                    .SelectMany(area => area.Cards)
                    .LongCount(minion => EffectiveCards.Kind(minion, world.Facts) == CardKind.Minion));
        return reduction;
    }
}
