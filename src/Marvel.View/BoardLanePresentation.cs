namespace Marvel.View;

/// <summary>One scenario, player, or fallback board lane.</summary>
public sealed record BoardLanePresentation(
    string Key,
    string Title,
    int? Seat,
    IReadOnlyList<BoardAreaPresentation> Areas);
