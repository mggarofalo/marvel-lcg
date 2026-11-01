using static Marvel.Cards.Dsl.AbilityBookLowering;
using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Lowers the optional fields of an encounter-deck discard-until instruction.</summary>
internal static class AbilityEncounterDiscardLowering
{
    // Engine spelling: "revealStatus" names the status a revealing sentence gives its match.
    internal static string? RevealStatus(
        IReadOnlyDictionary<string, AbilityValue> fields, AbilityLocation location, AbilityDiscardUntilResult result)
    {
        if (!fields.TryGetValue("revealStatus", out var value)) return null;
        if (result != AbilityDiscardUntilResult.Reveal)
            throw location.Child("revealStatus").Error("revealStatus requires reveal");
        string status = Text(value, location.Child("revealStatus"));
        return status is Statuses.Tough or Statuses.Stunned or Statuses.Confused ? status
            : throw location.Child("revealStatus").Error($"'{status}' is not a status");
    }

    internal static string? Set(
        IReadOnlyDictionary<string, AbilityValue> fields, AbilityLocation location, AbilityDiscardUntilResult result)
    {
        bool set = fields.ContainsKey("set");
        if (set && (fields.ContainsKey("kind") || fields.ContainsKey("trait")))
            throw location.Error("encounter discardUntil requires either a set or a kind and optional trait");
        if (set && result != AbilityDiscardUntilResult.Reveal)
            throw location.Error("encounter set discardUntil requires reveal");
        string? setName = OptionalText(fields, "set", location);
        if (set && string.IsNullOrEmpty(setName))
            throw location.Child("set").Error("expected a nonempty set");
        return setName;
    }
}
