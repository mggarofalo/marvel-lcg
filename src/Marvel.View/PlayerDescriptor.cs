using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Public information about one seat.</summary>
public sealed record PlayerDescriptor(int Seat, string Name, bool Eliminated)
{
    /// <summary>The id of the game area holding this seat's play area, or null when it is in none.</summary>
    public int? GameArea { get; init; }
}
