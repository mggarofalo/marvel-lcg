namespace Marvel.Godot;

/// <summary>Visibility-safe inputs for choosing one expanded tabletop workspace.</summary>
/// <remarks>
/// <see cref="SessionKey"/> is supplied by the host/client session boundary. The desktop
/// deliberately does not derive any seat role from cards, prompt text, or the visible board.
/// </remarks>
internal sealed record DisplayedSeatSnapshot(
    string SessionKey,
    IReadOnlyList<int> Seats,
    DisplayedSeatRoles? Roles);
