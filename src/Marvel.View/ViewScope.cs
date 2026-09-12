using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Marvel.Server")]

namespace Marvel.View;

/// <summary>The server-owned decision about which private seats a session may see.</summary>
public sealed class ViewScope
{
    private readonly HashSet<int> seats;

    internal ViewScope(IEnumerable<int> seats) =>
        this.seats = new HashSet<int>(seats);

    /// <summary>A scope with no private-seat access.</summary>
    public static ViewScope None { get; } = new([]);

    /// <summary>Whether private information belonging to a seat may be returned.</summary>
    public bool Includes(int seat) => seats.Contains(seat);

    /// <summary>The sole authorized seat, or null for a broader or empty scope.</summary>
    internal int? SoleSeat => seats.Count == 1 ? seats.Single() : null;

    /// <summary>Whether this scope authorizes exactly one specified seat.</summary>
    internal bool IsExactly(int seat) => seats.Count == 1 && seats.Contains(seat);
}
