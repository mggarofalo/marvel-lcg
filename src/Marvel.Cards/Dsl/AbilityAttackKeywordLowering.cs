using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

internal static class AbilityAttackKeywordLowering
{
    internal static void Validate(CardAbility ability, int ordinal,
        IEnumerable<KeyValuePair<AbilityEffectAddress, AbilityEffect>> effects, AbilityLocation location)
    {
        if (ability.Trigger.Timing == AbilityType.Constant && ability.When is null && ability.Cost is null
            && ability.Limit is null && ability.Maximum is null) return;
        if (effects.Any(entry => entry.Key.Card == ability.Card && entry.Key.Ability == ordinal
            && entry.Value is AbilityEffect.GrantField { AttackRestriction: not null }))
            throw location.Error("attackScope requires an unconditional, cost-free constant");
    }

    internal static AttackKeywordRestriction? Restriction(IReadOnlyDictionary<string, AbilityValue> fields,
        AbilityLocation location, AbilityCardSelection cards, string keyword, bool each)
    {
        if (!fields.TryGetValue("attackScope", out var value)) return null;
        var restriction = Lower(value, location.Child("attackScope"), keyword);
        if (restriction is AttackKeywordRestriction.Source
            && (each || cards is not AbilityCardSelection.Bound { Binding: AbilityCardBinding.This }))
            throw location.Error("source attackScope must grant to this card");
        return restriction;
    }

    internal static AttackKeywordRestriction Lower(AbilityValue value, AbilityLocation location, string keyword)
    {
        if (keyword is not ("piercing" or "ranged" or "overkill"))
            throw location.Error("attackScope requires an attack keyword");
        if (value is AbilityValue.Word { Value: "basic" }) return new AttackKeywordRestriction.Basic();
        if (value is AbilityValue.Word { Value: "source" }) return new AttackKeywordRestriction.Source();
        var fields = Fields(value, location, "sourceTrait");
        string trait = Text(Required(fields, "sourceTrait", location), location.Child("sourceTrait"));
        if (string.IsNullOrWhiteSpace(trait)) throw location.Error("sourceTrait must be nonempty");
        return new AttackKeywordRestriction.SourceTrait(trait);
    }
}
