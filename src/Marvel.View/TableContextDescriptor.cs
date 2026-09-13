namespace Marvel.View;

/// <summary>Seats with distinct table roles in one authorized response.</summary>
/// <remarks>
/// These roles are deliberately not inferred from labels, a selected player area, or
/// the first-player token. The tabletop rules do not prescribe this wire shape; the
/// engine chooses it so a client can change its visual focus without changing authority.
/// </remarks>
public sealed record TableContextDescriptor(
    int? PromptOwner,
    int? ViewedPrivateSeat,
    int ActivePlayer,
    int FirstPlayer,
    int PublicFocusSeat);
