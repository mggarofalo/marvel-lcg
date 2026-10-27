using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Shared exhaustion affordability using current or projected quantities.</summary>
internal static class AbilityExhaustionCostAvailability
{
    internal static bool CanPay(World world, Card source, int player, AbilityCost? cost,
        AbilityQueryProjection? projection = null) => cost switch
    {
        AbilityCost.Sequence sequence => sequence.Costs.All(step => CanPay(world, source, player, step, projection)),
        AbilityCost.Exhaust exhaust => AbilityPaymentRules.CostTarget(world, source, player, exhaust.Card)
            is { } card && Ready(card, projection),
        AbilityCost.ExhaustTitled named => NamedAbilityCost.Target(world, player, named.Title)
            is { } card && Ready(card, projection),
        AbilityCost.ExhaustChosen chosen => AbilityCostSelection.Choices(world, player, chosen)
            .Count(card => Ready(card, projection)) >= AbilityCostSelection.Range(chosen.Range, int.MaxValue).Min,
        _ => true,
    };

    private static bool Ready(Card card, AbilityQueryProjection? projection) =>
        (projection?.Quantities(card) ?? card.InstanceState).Ready;
}
