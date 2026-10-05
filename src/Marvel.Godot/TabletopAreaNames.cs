namespace Marvel.Godot;

/// <summary>Quiet display names for projected table navigation.</summary>
internal static class TabletopAreaNames
{
    internal static string Title(string title)
    {
        string text = title.ToLowerInvariant();
        return text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
    }

    internal static string Region(string zone) => zone switch
    {
        "SupportsArea" => "Supports", "UpgradesArea" => "Upgrades",
        "AlliesArea" => "Allies", "EngagedEnemiesArea" => "Engaged enemies",
        "SideSchemesArea" => "Side schemes", "RevealingArea" => "Current encounter",
        "BoostCardsDeck" => "Boost",
        _ => "Cards",
    };
}
