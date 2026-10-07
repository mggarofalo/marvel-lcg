using System.Collections.Immutable;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;

namespace Marvel.Cards.Dsl;

// Intrinsic definitions are compiled as definitions, never inferred from text.
internal static class AbilityBaseDefinitions
{
    internal static AbilityEffect.DefineBaseValue Lower(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "field", "value");
        string field = Text(Required(fields, "field", location), location.Child("field"));
        if (!StateFieldCatalog.IsBasicPowerField(field))
            throw location.Child("field").Error($"'{field}' is not a supported basic-power base");
        return new(field, Number(Required(fields, "value", location), location.Child("value")));
    }

    internal static void Validate(
        ImmutableArray<CompiledCardAbility> abilities,
        ImmutableDictionary<AbilityEffectAddress, AbilityEffect> effects)
    {
        var defined = new HashSet<(string Card, string Field)>();
        foreach (CompiledCardAbility ability in abilities)
        {
            if (ability.Effect is not AbilityEffect.DefineBaseValue definition) continue;
            var location = Location(ability.Address);
            if (ability.Trigger.Timing != AbilityType.Constant || ability.Cost is not null || ability.When is not null)
                throw location.Error("a base definition must be an unconditional, cost-free constant");
            if (!defined.Add((ability.Card, definition.Field)))
                throw location.Error($"duplicate base definition for '{definition.Field}'");
        }
        foreach (var (address, effect) in effects.OrderBy(pair => pair.Key.Card, StringComparer.Ordinal)
                     .ThenBy(pair => pair.Key.Ability).ThenBy(pair => pair.Key.Path, StringComparer.Ordinal))
        {
            if (effect is AbilityEffect.DefineBaseValue && address.Path != "effect")
                throw Location(address).Error("base definitions must be at the constant ability root");
        }
    }

    private static AbilityLocation Location(AbilityEffectAddress address) =>
        new(address.Card, address.Ability, address.Path);
}
