using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;

namespace Marvel.Cards.Dsl;

/// <summary>Lowers labeled powers and validates their declared recipient contracts.</summary>
internal static class AbilityPowerLowering
{
    internal static AbilityEffect.Power PowerEffect(
        AbilityValue value, AbilityLocation location, AbilityPowerKind kind)
    {
        var fields = kind == AbilityPowerKind.Defense ? Fields(value, location, "effect")
            : kind == AbilityPowerKind.Thwart ? Fields(value, location, "target", "effect", "automaticTarget")
                : Fields(value, location, "target", "effect", "count");
        bool automatic = Marker(fields, "automaticTarget", location);
        var body = LowerEffect(Required(fields, "effect", location), location.Child("effect"));
        var count = AttackCount(fields, body, location);
        var target = PowerTarget(fields, body, kind, automatic, location);
        AbilityStagedAttackLowering.Validate(body, kind, target, count, location);
        return new(kind, target, body, automatic) { TargetCount = count };
    }

    private static AbilityNumber? AttackCount(
        IReadOnlyDictionary<string, AbilityValue> fields, AbilityEffect body, AbilityLocation location)
    {
        var count = fields.ContainsKey("count") ? Numeric(fields, "count", location) : null;
        if (count is not (null or AbilityNumber.Constant
            or AbilityNumber.ResolutionValue { Kind: AbilityResolutionNumber.CardsDiscarded }))
            throw location.Child("count").Error("a counted attack requires a constant or physical discard-receipt count");
        if (count is not null && body is not AbilityEffect.AttackDamage
            { SimultaneousPlacement: true, Amount: AbilityNumber.Constant,
              Cards: AbilityCardSelection.Query { Kind: AbilityCardQuery.PowerTargets } })
            throw location.Child("effect").Error("a counted attack requires one simultaneous constant-damage effect over powerTargets");
        return count;
    }

    private static AbilityCardSelection? PowerTarget(
        IReadOnlyDictionary<string, AbilityValue> fields, AbilityEffect body,
        AbilityPowerKind kind, bool automatic, AbilityLocation location)
    {
        if (kind == AbilityPowerKind.Defense) return null;
        if (kind == AbilityPowerKind.Attack && !fields.ContainsKey("target")
            && AbilityStagedAttackLowering.IsSplashBody(body)) return null;
        if (kind != AbilityPowerKind.Thwart || fields.ContainsKey("target"))
            return Selected(fields, "target", location);
        if (automatic || body is not AbilityEffect.GiveStatus
            { Cards: AbilityCardSelection.Query { Kind: AbilityCardQuery.Villain } })
            throw location.Error("a scheme-less thwart requires one independent villain status effect");
        return null;
    }
}
