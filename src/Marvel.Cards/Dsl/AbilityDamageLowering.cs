using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;

namespace Marvel.Cards.Dsl;

/// <summary>Validates authored damage, movement and prevention instruction shapes.</summary>
internal static class AbilityDamageLowering
{
    internal static AbilityEffect.Damage DamageEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "cards", "amount", "attack", "ignoresTough");
        return new(Selected(fields, "cards", location), Numeric(fields, "amount", location), Marker(fields, "attack", location))
        { IgnoresTough = Marker(fields, "ignoresTough", location) };
    }

    internal static AbilityEffect.AttackDamage AttackDamageEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "cards", "amount", "overkill", "simultaneous");
        return new(Selected(fields, "cards", location), Numeric(fields, "amount", location), Marker(fields, "overkill", location))
        { SimultaneousPlacement = Marker(fields, "simultaneous", location) };
    }

    internal static AbilityEffect.MoveDamage MoveDamageEffect(AbilityValue value, AbilityLocation location, bool attack)
    {
        var fields = Fields(value, location, "from", "to", "amount");
        return new(Selected(fields, "from", location), Selected(fields, "to", location), Numeric(fields, "amount", location), attack);
    }

    internal static AbilityEffect.IndirectDamage IndirectEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "among", "amount");
        return new(Selected(fields, "among", location), Numeric(fields, "amount", location));
    }

    internal static AbilityEffect.PreventDamage PreventDamageEffect(AbilityValue value, AbilityLocation location)
    {
        if (value is AbilityValue.Word)
        {
            FixedWord(value, location, "trigger.target");
            return new(new AbilityNumber.Constant(long.MaxValue));
        }
        var fields = Fields(value, location, "card", "amount");
        FixedWord(Required(fields, "card", location), location.Child("card"), "trigger.target");
        return new(fields.ContainsKey("amount") ? Numeric(fields, "amount", location) : new AbilityNumber.Constant(long.MaxValue));
    }

}
