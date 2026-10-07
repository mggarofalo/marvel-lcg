namespace Marvel.Godot;

/// <summary>Maps card galleries to persistent draft controls after a render replaces their nodes.</summary>
internal static class CardChoiceFocus
{
    internal static string? Key(string name) => name.StartsWith("SearchResult", StringComparison.Ordinal)
        ? "Affordance" + name["SearchResult".Length..]
        : name.StartsWith("MinionOrderCard", StringComparison.Ordinal)
            ? "Target" + name["MinionOrderCard".Length..] : null;

    internal static bool IsPage(string name) => name is "PreviousSearchPage" or "NextSearchPage"
        or "PreviousMinionPage" or "NextMinionPage";

    internal static string? PairedPage(string name) => name switch
    {
        "PreviousSearchPage" => "NextSearchPage", "NextSearchPage" => "PreviousSearchPage",
        "PreviousMinionPage" => "NextMinionPage", "NextMinionPage" => "PreviousMinionPage",
        _ => null,
    };
}
