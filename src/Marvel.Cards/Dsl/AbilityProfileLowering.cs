using System.Collections.Immutable;
using Marvel.Rules.State;
using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using static Marvel.Cards.Dsl.AbilityConditionLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;

namespace Marvel.Cards.Dsl;

internal static class AbilityProfileLowering
{
    internal static EffectiveCardProfile Profile(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "id", "title", "kind", "traits", "baseValues");
        string id = Text(Required(fields, "id", location), location.Child("id"));
        string title = Text(Required(fields, "title", location), location.Child("title"));
        if (id.Length == 0 || title.Length == 0) throw location.Error("profile id and title must be nonempty");
        var kind = ConditionCardKind(Required(fields, "kind", location), location.Child("kind"));
        if (kind != CardKind.Minion) throw location.Child("kind").Error("only blank minion profiles are supported");
        if (Required(fields, "traits", location) is not AbilityValue.List traits)
            throw location.Child("traits").Error("expected a trait list");
        if (Required(fields, "baseValues", location) is not AbilityValue.Map values)
            throw location.Child("baseValues").Error("expected base values");
        var numbers = ImmutableDictionary.CreateBuilder<string, long>(StringComparer.Ordinal);
        foreach (var (name, amount) in values.Entries)
        {
            if (name is not ("SCH" or "ATK" or "HP"))
                throw location.Child("baseValues/" + name).Error("unsupported minion base value");
            numbers.Add(name, NonnegativeCount(amount, location.Child("baseValues/" + name)));
        }
        if (!numbers.TryGetValue("HP", out long health) || health <= 0)
            throw location.Child("baseValues/HP").Error("a minion profile needs positive hit points");
        return new(id, title, kind,
            [.. traits.Values.Select((trait, index) => Text(trait, location.Child("traits").Item(index)))],
            numbers.ToImmutable());
    }

    internal static AbilityEffect.EngageTopAsMinion Engage(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "player", "count", "profile");
        return new(Players(Required(fields, "player", location), location.Child("player")),
            NonnegativeCount(Required(fields, "count", location), location.Child("count")),
            Text(Required(fields, "profile", location), location.Child("profile")));
    }
}
