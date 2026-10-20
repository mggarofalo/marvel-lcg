using static Marvel.Cards.Dsl.AbilityBookLowering;
using static Marvel.Cards.Dsl.AbilityCostLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;

namespace Marvel.Cards.Dsl;

internal static class AbilityHitPointLowering
{
    internal static AbilityEffect.SetHitPoints Lower(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "card", "amount");
        return new(Selected(fields, "card", location),
            PositiveInteger(Required(fields, "amount", location), location.Child("amount")));
    }
}
