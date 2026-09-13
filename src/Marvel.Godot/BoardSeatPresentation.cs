using Marvel.View;

namespace Marvel.Godot;

/// <summary>One visibility-safe seat entry and the workspace it selects.</summary>
public sealed record BoardSeatPresentation(
    int Seat,
    string Name,
    string Summary,
    bool IsViewed,
    bool IsAnswering,
    bool IsFirstPlayer,
    int ActionCount,
    int LegalTargetCount,
    int SelectedTargetCount,
    BoardLanePresentation Lane,
    BoardAreaPresentation? Hand);
