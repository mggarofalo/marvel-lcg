using static Marvel.Cards.Dsl.AbilityCostLowering;
using static Marvel.Cards.Dsl.AbilityEffectLowering;

namespace Marvel.Cards.Dsl;

internal static class AbilitySearchFilters
{
    internal static AbilityCardSelection.InAreas Lower(AbilityCardSelection.InAreas selection,
        IReadOnlyDictionary<string, AbilityValue> fields, AbilityLocation location)
    {
        string? set = AbilitySelectorLowering.OptionalText(fields, "set", location);
        if (set is not null && string.IsNullOrWhiteSpace(set))
            throw location.Child("set").Error("expected a nonempty printed set");
        int? top = fields.TryGetValue("top", out var count) ? PositiveCount(count, location.Child("top")) : null;
        if (top is not null && (selection.Areas.Length != 1
            || selection.Areas[0] is not (AbilitySearchArea.YourDeck or AbilitySearchArea.EncounterDeck)))
            throw location.Child("top").Error("top-card searches require one deck");
        return selection with
        {
            Top = top, AspectOnly = Boolean(fields, "aspectOnly", location),
            CardClass = AbilitySelectorLowering.OptionalText(fields, "cardClass", location),
            Set = set,
            DifferentTitleFromRevealed = Boolean(fields, "differentTitleFromRevealed", location),
        };
    }
}
