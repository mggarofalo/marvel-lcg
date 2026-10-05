using System.Globalization;
using Marvel.Rules.State;
using static Marvel.Cards.Dsl.AbilityBookLowering;

namespace Marvel.Cards.Dsl;

// After semantic lowering, resolve assignment ids across every authored subtree.
// This walks inert syntax once at compilation, never during a live query.
internal static class AbilityProfileReferences
{
    internal static void Validate(AbilityBook book, IReadOnlyDictionary<string, EffectiveCardProfile> profiles)
    {
        var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var ability in book.Abilities)
        {
            int ordinal = ordinals.GetValueOrDefault(ability.Card);
            ordinals[ability.Card] = ordinal + 1;
            var location = new AbilityLocation(ability.Card, ordinal, "effect");
            Visit(Syntax(ability.Effect), location, profiles);
            if (ability.Cost is { } cost) Visit(Syntax(cost), location with { Path = "cost" }, profiles);
            if (ability.When is { } when) Visit(Syntax(when), location with { Path = "when" }, profiles);
        }
        foreach (var (card, selection) in (book.AttachTo ?? new Dictionary<string, AbilityValue>())
                     .OrderBy(pair => pair.Key, StringComparer.Ordinal))
            Visit(selection, new(card, 0, "attachTo"), profiles);
    }

    private static void Visit(AbilityValue value, AbilityLocation location,
        IReadOnlyDictionary<string, EffectiveCardProfile> profiles)
    {
        if (value is AbilityValue.List list)
        {
            for (int i = 0; i < list.Values.Count; i++)
                Visit(list.Values[i], location.Child(i.ToString(CultureInfo.InvariantCulture)), profiles);
        }
        else if (value is AbilityValue.Map map)
        {
            foreach (var (key, child) in map.Entries.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                var nested = location.Child(key);
                if (key == "defeatedWithProfile" && child is AbilityValue.Map fields
                    && fields.Entry("profile") is AbilityValue.Word id && !profiles.ContainsKey(id.Value))
                    throw nested.Child("profile").Error($"unknown profile '{id.Value}'");
                Visit(child, nested, profiles);
            }
        }
    }
}
