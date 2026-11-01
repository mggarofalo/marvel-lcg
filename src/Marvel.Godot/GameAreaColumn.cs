namespace Marvel.Godot;

/// <summary>One column of the separated table: the central stage or one game area.</summary>
/// <param name="GameArea">The engine's game-area id, or null for the central column.</param>
/// <param name="Heading">The engine-authored area label, or the client's central caption.</param>
/// <param name="Markers">Typed facts, in display order.</param>
/// <param name="Details">Engine-authored sentences that explain the markers, in display order.</param>
/// <param name="Villains">Villain object ids placed in this column.</param>
/// <param name="Schemes">Main scheme object ids placed in this column.</param>
/// <param name="Encounter">Side scheme and environment object ids placed in this column.</param>
internal sealed record GameAreaColumn(
    int? GameArea,
    string Heading,
    IReadOnlyList<GameAreaMarker> Markers,
    IReadOnlyList<string> Details,
    IReadOnlyList<int> Villains,
    IReadOnlyList<int> Schemes,
    IReadOnlyList<int> Encounter)
{
    /// <summary>The display words for this column's markers; the wording is our choice.</summary>
    internal string MarkerText => string.Join(" · ", Markers.Select(Word));

    /// <summary>The display word for one marker.</summary>
    internal static string Word(GameAreaMarker marker) => marker switch
    {
        GameAreaMarker.Yours => "YOUR AREA",
        GameAreaMarker.Acting => "ACTING NOW",
        GameAreaMarker.Retained => "RETAINED",
        GameAreaMarker.Departing => "LEAVES AT PHASE END",
        GameAreaMarker.OutOfReach => "OUT OF REACH",
        _ => throw new ArgumentOutOfRangeException(nameof(marker), marker, "no word for this marker"),
    };
}
