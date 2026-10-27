using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>Admits the bounded adjusted printed-price restriction.</summary>
internal static class AbilityPrintedCostResource
{
    internal static char? Read(JsonElement element)
    {
        if (!element.TryGetProperty("printedCostResource", out var value)) return null;
        if (value.ValueKind != JsonValueKind.String || value.GetString() != "R")
            throw new AbilityException("printedCostResource supports only physical (R)");
        return Resources.Physical;
    }

    internal static char? Validate(CardAbility ability)
    {
        if (ability.PrintedCostResource is { } resource
            && (resource != Resources.Physical || ability.Trigger.Timing != AbilityType.Action))
            throw new AbilityException("printedCostResource requires a physical-only action price");
        return ability.PrintedCostResource;
    }
}
