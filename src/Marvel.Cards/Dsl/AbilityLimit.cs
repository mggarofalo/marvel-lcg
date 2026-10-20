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

    internal static AbilityLimitPeriod Validate(CardAbility ability)
    {
        if (!Enum.IsDefined(ability.LimitPeriod) || ability.Limit is <= 0
            || (ability.Limit is null && ability.LimitPeriod != AbilityLimitPeriod.Round))
            throw new AbilityException("an ability limit needs a positive count and a supported period");
        return ability.LimitPeriod;
    }
}
