using Marvel.Rules.State;
using static Marvel.Cards.Dsl.AbilityModifierLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using static Marvel.Cards.Dsl.AbilityCostLowering;
using static Marvel.Cards.Dsl.AbilityLowering;

namespace Marvel.Cards.Dsl;

/// <summary>Strict status-kind vocabulary for explicit removal effects.</summary>
internal static class AbilityStatusRemovalLowering
{
    internal static AbilityEffect.RemoveStatuses Lower(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "cards", "statuses");
        if (Required(fields, "statuses", location) is not AbilityValue.List { Values.Count: > 0 } list)
            throw location.Child("statuses").Error("expected a nonempty list of status kinds");
        var statuses = list.Values.Select(item => Text(item, location.Child("statuses"))).ToArray();
        if (statuses.Any(status => status is not (Statuses.Tough or Statuses.Stunned or Statuses.Confused))
            || statuses.Distinct(StringComparer.Ordinal).Count() != statuses.Length)
            throw location.Child("statuses").Error("expected distinct supported status kinds");
        return new(SelectCards(Required(fields, "cards", location), location.Child("cards")), [.. statuses]);
    }
}


