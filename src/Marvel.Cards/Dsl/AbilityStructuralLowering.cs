using static Marvel.Cards.Dsl.AbilityStructuralLowering;
using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilityBookLowering;
using static Marvel.Cards.Dsl.AbilityConditionLowering;
using static Marvel.Cards.Dsl.AbilityCostLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;
using static Marvel.Cards.Dsl.AbilityModifierLowering;
using static Marvel.Cards.Dsl.AbilityProcedureLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

internal static class AbilityStructuralLowering
{
    internal static ImmutableArray<AbilityEffect> Effects(AbilityValue value, AbilityLocation location)
    {
        if (value is not AbilityValue.List list)
        {
            throw location.Error("expected a list of effects");
        }
        var builder = ImmutableArray.CreateBuilder<AbilityEffect>(list.Values.Count);
        for (int index = 0; index < list.Values.Count; index++)
        {
            builder.Add(LowerEffect(list.Values[index], location.Item(index)));
        }
        return builder.MoveToImmutable();
    }

    internal static AbilityEffect OnlyEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "effect");
        return LowerEffect(Required(fields, "effect", location), location.Child("effect"));
    }

    internal static AbilityEffect.Conditional ConditionalEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "test", "then", "else");
        return new(LowerCondition(Required(fields, "test", location), location.Child("test")),
            OptionalEffect(fields, "then", location), OptionalEffect(fields, "else", location));
    }

    internal static AbilityEffect? OptionalEffect(
        IReadOnlyDictionary<string, AbilityValue> fields, string name, AbilityLocation location) =>
        fields.TryGetValue(name, out var value) ? LowerEffect(value, location.Child(name)) : null;

    internal static AbilityEffect.Dependent DependentEffect(AbilityValue value, AbilityLocation location, string kind)
    {
        var fields = Fields(value, location, "effect", kind);
        return new(LowerEffect(Required(fields, "effect", location), location.Child("effect")),
            LowerEffect(Required(fields, kind, location), location.Child(kind)), kind == "then");
    }

    internal static AbilityEffect.ForEach RepeatedEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "count", "effect");
        return new(Number(Required(fields, "count", location), location.Child("count")),
            LowerEffect(Required(fields, "effect", location), location.Child("effect")));
    }

    internal static AbilityEffect.EachTime EachTimeEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "effect", "when", "then");
        return new(LowerEffect(Required(fields, "effect", location), location.Child("effect")),
            LowerCondition(Required(fields, "when", location), location.Child("when")),
            LowerEffect(Required(fields, "then", location), location.Child("then")));
    }

    internal static AbilityEffect.Choose ChooseEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "options", "descriptions");
        var options = Effects(Required(fields, "options", location), location.Child("options"));
        if (options.Length < 2)
        {
            throw location.Child("options").Error("expected at least two choice options");
        }
        ImmutableArray<string> descriptions = [];
        if (fields.TryGetValue("descriptions", out var authored))
        {
            if (authored is not AbilityValue.List list || list.Values.Count != options.Length)
            {
                throw location.Child("descriptions").Error("expected one description per option");
            }
            descriptions = [.. list.Values.Select((item, index) => Text(item, location.Child("descriptions").Item(index)))];
        }
        return new(options, descriptions);
    }

    internal static AbilityEffect.ChooseCard ChooseCardEffect(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "from", "effect");
        return new(Selected(fields, "from", location),
            LowerEffect(Required(fields, "effect", location), location.Child("effect")));
    }

}
