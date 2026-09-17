using Marvel.View;

namespace Marvel.Godot;

/// <summary>Identifies zones and state with explicit places in the spatial grammar.</summary>
internal static class SpatialTableZones
{
    internal static BoardCardPresentation[] Current(BoardAreaPresentation area) => [.. area.Cards
        .Where(card => card.StageRole != BoardStageRole.Upcoming)
        .Concat(area.Removed)];

    internal static bool IsExhausted(BoardCardPresentation card) =>
        card.Status.Contains("exhaust", StringComparison.OrdinalIgnoreCase);

    internal static readonly HashSet<string> Known = new(StringComparer.Ordinal)
    {
        "EncounterDiscardPile", "EncounterDeck", "MainSchemesArea", "VillainArea",
        "SideSchemesArea", "RevealingArea", "DiscardPile", "PlayerDeck",
        "EngagedEnemiesArea", "HeroArea", "AlliesArea", "SupportsArea", "UpgradesArea",
    };
}
