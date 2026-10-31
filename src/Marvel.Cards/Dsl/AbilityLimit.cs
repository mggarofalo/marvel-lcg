using System.Text.Json;

namespace Marvel.Cards.Dsl;

internal static class AbilityLimit
{
    internal static long? ReadCount(JsonElement element)
    {
        bool round = element.TryGetProperty("limitPerRound", out var roundValue);
        bool phase = element.TryGetProperty("limitPerPhase", out var phaseValue);
        if (!round && !phase) return null;
        if (round && phase) throw new AbilityException("an ability must name one limit period");
        var value = phase ? phaseValue : roundValue;
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out long count) || count <= 0)
            throw new AbilityException("an ability limit must be a positive integer");
        return count;
    }

    internal static string? ReadGroup(JsonElement element)
    {
        if (!element.TryGetProperty("limitGroup", out var value)) return null;
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new AbilityException("a limit group must be a nonempty string");
        return value.GetString();
    }

    /// <summary>A shared group needs one limit on every member, in more than one ability on its face.</summary>
    /// <remarks>
    /// One printed ability with several triggering conditions is several rows
    /// whose single printed limit counts across all of them (rr:limit: "each copy
    /// of an ability with such a limit may be used X times per the specified
    /// period"). The group spelling is an engine choice.
    /// </remarks>
    internal static void ValidateGroups(IEnumerable<CompiledCardAbility> abilities)
    {
        foreach (var group in abilities.Where(ability => ability.LimitGroup is not null)
            .GroupBy(ability => (ability.Card, ability.LimitGroup)))
        {
            var members = group.ToList();
            if (members.Count < 2 || members.Any(member => member.Limit is null
                || member.Limit != members[0].Limit || member.LimitPeriod != members[0].LimitPeriod))
                throw new AbilityException(
                    $"limit group '{group.Key.LimitGroup}' on '{group.Key.Card}' needs one shared limit on several abilities");
        }
    }

    internal static AbilityLimitPeriod Validate(CardAbility ability)
    {
        if (!Enum.IsDefined(ability.LimitPeriod) || ability.Limit is <= 0
            || (ability.Limit is null && ability.LimitPeriod != AbilityLimitPeriod.Round))
            throw new AbilityException("an ability limit needs a positive count and a supported period");
        return ability.LimitPeriod;
    }
}
