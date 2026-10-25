using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using static Marvel.Cards.Dsl.AbilityCostLowering;
using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilityBookLowering;
using static Marvel.Cards.Dsl.AbilityModifierLowering;

namespace Marvel.Cards.Dsl;

/// <summary>Strict authoring contract for live trait-qualified combat bonuses.</summary>
internal static class AbilityTraitGroupLowering
{
    internal static AbilityEffect.GrantTraitCharacters Lower(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "trait", "fields", "amount", "until");
        if (Required(fields, "fields", location) is not AbilityValue.List { Values.Count: > 0 } list)
            throw location.Child("fields").Error("expected combat fields");
        var names = list.Values.Select(item => Text(item, location.Child("fields"))).ToArray();
        if (names.Any(name => name is not "attack" and not "thwart"))
            throw location.Child("fields").Error("only attack and thwart are supported for a live trait set");
        if (names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            throw location.Child("fields").Error("combat fields must be distinct");
        return new(Text(Required(fields, "trait", location), location.Child("trait")), [.. names],
            PositiveCount(Required(fields, "amount", location), location.Child("amount")),
            TimingPoint(Required(fields, "until", location), location.Child("until")));
    }
}
