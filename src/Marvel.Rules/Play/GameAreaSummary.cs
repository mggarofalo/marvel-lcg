using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>What one game area holds, for a client to draw it as its own region.</summary>
/// <param name="Area">The game area.</param>
/// <param name="Villains">The villains in play in this area, by object id.</param>
/// <param name="MainSchemes">The main schemes in play in this area, by object id.</param>
/// <param name="Central">Whether the area holds the villain's play area.</param>
/// <param name="Retained">
/// Whether the area has no players but still holds an eliminated player's local
/// components until their printed phase-end departure.
/// </param>
/// <param name="FormerSeats">Eliminated seats whose retained components this area still holds.</param>
/// <param name="Label">The engine-authored display name: its players, or its central role, and its villains.</param>
public sealed record GameAreaSummary(
    GameArea Area,
    IReadOnlyList<int> Villains,
    IReadOnlyList<int> MainSchemes,
    bool Central,
    bool Retained,
    IReadOnlyList<int> FormerSeats,
    string Label);
