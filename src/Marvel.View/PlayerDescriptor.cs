namespace Marvel.View;

/// <summary>Public information about one seat.</summary>
public sealed record PlayerDescriptor(int Seat, string Name, bool Eliminated)
{
    /// <summary>The id of the game area holding this seat's play area, or null when it is in none.</summary>
    public int? GameArea { get; init; }

    /// <summary>
    /// Addressable cards in play that this seat's cards cannot affect, because they are in another
    /// game area (<c>pack:mc11:game-areas</c>). Empty while every card shares one game area.
    /// </summary>
    public IReadOnlyList<int> OutOfReach { get; init; } = [];
}
