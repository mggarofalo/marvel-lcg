namespace Marvel.Godot;

/// <summary>One expanded player workspace and the distinct authoritative roles shown beside it.</summary>
internal sealed record DisplayedSeatSelection(
    int ExpandedSeat,
    int? ActivePlayer,
    int? PromptOwner,
    int? ViewedPrivateSeat,
    int? PublicFocusSeat);
