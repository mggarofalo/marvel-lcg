using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>Lowers the selection of status cards on one bound character.</summary>
internal static class AbilityStatusSelectorLowering
{
    // Engine choice: the object form restricts the host's status cards to named kinds.
    internal static AbilityCardSelection.StatusCardsOn StatusCardsOn(AbilityValue value, AbilityLocation location)
    {
        bool filtered = value is AbilityValue.Map map && map.Entries.ContainsKey("statuses");
        var fields = filtered ? Fields(value, location, "card", "statuses") : null;
        var host = SelectCards(fields is null ? value : Required(fields, "card", location),
            fields is null ? location : location.Child("card")) as AbilityCardSelection.Bound
            ?? throw location.Error("expected a card binding for the status host");
        if (fields is null) return new(host.Binding);
        return new(host.Binding) { Statuses = Kinds(Required(fields, "statuses", location), location.Child("statuses")) };
    }

    private static ImmutableArray<string> Kinds(AbilityValue value, AbilityLocation location)
    {
        if (value is not AbilityValue.List { Values.Count: > 0 } list)
            throw location.Error("expected a nonempty list of status kinds");
        ImmutableArray<string> statuses = [.. list.Values.Select((item, index) => Kind(item, location.Item(index)))];
        return statuses.Distinct(StringComparer.Ordinal).Count() == statuses.Length ? statuses
            : throw location.Error("status kinds must be distinct");
    }

    private static string Kind(AbilityValue value, AbilityLocation location) =>
        Text(value, location) is var status && status is Statuses.Tough or Statuses.Stunned or Statuses.Confused
            ? status : throw location.Error($"'{status}' is not a status kind");
}
