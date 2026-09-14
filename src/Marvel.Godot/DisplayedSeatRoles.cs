namespace Marvel.Godot;

/// <summary>Authoritative seat roles copied from a visibility-safe table contract.</summary>
/// <remarks>
/// The board controller maps the transport-neutral contract into this presentation value.
/// Keeping that adaptation at the composition boundary avoids making this local state
/// collaborator another consumer of the shared wire type.
/// </remarks>
internal sealed record DisplayedSeatRoles(
    int? PromptOwner,
    int? ViewedPrivateSeat,
    int? ActivePlayer,
    int? PublicFocusSeat);
