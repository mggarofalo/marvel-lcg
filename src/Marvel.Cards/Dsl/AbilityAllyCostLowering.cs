using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

internal static class AbilityAllyCostLowering
{
    internal static void Validate(CardAbility ability, AbilityEffect root, AbilityLocation location,
        IEnumerable<AbilityEffect> effects)
    {
        if (!effects.Any(effect => effect is AbilityPricingEffect.ReduceFirstAllyCost or AbilityPricingEffect.ReduceUpgradeCostOnThis or AbilityPricingEffect.ReduceUpgradeCostOnThis)) return;
        bool direct = root is AbilityPricingEffect.ReduceFirstAllyCost or AbilityPricingEffect.ReduceUpgradeCostOnThis or AbilityPricingEffect.ReduceUpgradeCostOnThis;
        bool unconditional = ability.Trigger == new AbilityTrigger(null, AbilityType.Constant, "this")
            && ability is { When: null, Cost: null, Limit: null, Maximum: null, AnyPlayer: false };
        if (!direct || !unconditional)
            throw location.Error("host/ally cost reduction requires a direct unconditional Constant ability");
    }
}
