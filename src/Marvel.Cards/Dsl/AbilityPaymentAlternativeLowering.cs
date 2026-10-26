using static Marvel.Cards.Dsl.AbilityCostLowering;
using static Marvel.Cards.Dsl.AbilityProcedureLowering;
using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilityBookLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;

namespace Marvel.Cards.Dsl;

/// <summary>Lowers resource payments with a printed alternative effect.</summary>
internal static class AbilityPaymentAlternativeLowering
{
    internal static AbilityEffect.PayOrEffect PayAlternative(AbilityValue value, AbilityLocation location, bool exhaustOnly)
    {
        var fields = Fields(value, location, "resources", "generic", "otherwise");
        var otherwise = LowerEffect(Required(fields, "otherwise", location), location.Child("otherwise"));
        if (exhaustOnly && otherwise is not AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Exhaust })
        {
            throw location.Child("otherwise").Error("payOrExhaust requires an exhaust alternative");
        }
        string resources = fields.TryGetValue("resources", out var required)
            ? ResourceString(required, location.Child("resources")) : string.Empty;
        int generic = fields.TryGetValue("generic", out var count)
            ? PositiveCount(count, location.Child("generic")) : 0;
        if (resources.Length == 0 && generic == 0)
            throw location.Error("expected resources or a positive generic resource count");
        return new(resources, otherwise, exhaustOnly) { Generic = generic };
    }
}

