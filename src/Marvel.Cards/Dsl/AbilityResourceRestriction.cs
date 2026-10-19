using System.Text.Json;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>Admits the explicit resource-use restrictions supported by the payment engine.</summary>
internal static class AbilityResourceRestriction
{
    internal static ResourceRestriction Read(JsonElement element, AbilityType timing)
    {
        if (!element.TryGetProperty("resourceRestriction", out var value)) return ResourceRestriction.None;
        if (timing != AbilityType.Resource || value.ValueKind != JsonValueKind.String)
            throw new AbilityException("resourceRestriction requires a resource ability and a string value");
        return value.GetString() switch
        {
            "arrowEvent" => ResourceRestriction.ArrowEvent,
            "aspectCard" => ResourceRestriction.AspectCard,
            _ => throw new AbilityException("unknown resourceRestriction"),
        };
    }

    internal static ResourceRestriction Validate(CardAbility ability)
    {
        if (!Enum.IsDefined(ability.ResourceRestriction)
            || ability.ResourceRestriction != ResourceRestriction.None && ability.Trigger.Timing != AbilityType.Resource)
            throw new AbilityException("invalid resourceRestriction on compiled ability");
        return ability.ResourceRestriction;
    }
}
