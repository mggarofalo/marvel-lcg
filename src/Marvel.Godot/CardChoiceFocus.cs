namespace Marvel.Godot;

/// <summary>Maps card galleries to persistent draft controls after a render replaces their nodes.</summary>
internal static class CardChoiceFocus
{
    internal static string? Key(string name) => name.StartsWith("SearchResult", StringComparison.Ordinal)
        ? "Affordance" + name["SearchResult".Length..]
        : name.StartsWith("VisibleTargetCard", StringComparison.Ordinal)
            ? "Target" + name["VisibleTargetCard".Length..] : null;

    internal static bool IsPage(string name) => name is "PreviousSearchPage" or "NextSearchPage"
        or "PreviousTargetPage" or "NextTargetPage";

    internal static string? PairedPage(string name) => name switch
    {
        "PreviousSearchPage" => "NextSearchPage", "NextSearchPage" => "PreviousSearchPage",
        "PreviousTargetPage" => "NextTargetPage", "NextTargetPage" => "PreviousTargetPage",
        _ => null,
    };
}
