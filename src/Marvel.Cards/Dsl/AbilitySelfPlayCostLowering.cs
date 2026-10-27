using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

internal static class AbilitySelfPlayCostLowering
{
    internal static AbilityEffect LowerControlledTrait(AbilityValue value, AbilityLocation location)
    {
        var fields = AbilitySelectorLowering.Fields(value, location, "trait", "amount");
        return new AbilityPricingEffect.ReduceSelfCostPerControlledTrait(
            AbilityLowering.Text(AbilitySelectorLowering.Required(fields, "trait", location), location.Child("trait")),
            AbilityCostLowering.PositiveInteger(AbilitySelectorLowering.Required(fields, "amount", location), location.Child("amount")));
    }

    internal static void Validate(CardAbility ability, AbilityEffect root, AbilityLocation location,
        IEnumerable<AbilityEffect> effects)
    {
        if (!effects.Any(IsSelfCost)) return;
        bool direct = IsSelfCost(root);
        bool unconditional = ability.Trigger == new AbilityTrigger(null, AbilityType.Constant, "this")
            && ability is { When: null, Cost: null, Limit: null, Maximum: null, AnyPlayer: false };
        if (!direct || !unconditional)
            throw location.Error("self-cost reduction requires a direct unconditional Constant ability");
    }

    private static bool IsSelfCost(AbilityEffect effect) => effect is
        AbilityPricingEffect.ReduceSelfCostPerEngagedMinion or AbilityPricingEffect.ReduceSelfCostPerControlledTrait;
}
