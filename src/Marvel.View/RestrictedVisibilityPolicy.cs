using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Marvel.Server")]

namespace Marvel.View;

/// <summary>Non-cooperative policy that binds the process to one authorized seat.</summary>
/// <remarks>
/// Viewer claims are validated as input but never select or narrow authority.
/// The server operator's configured seat is the only authority for the opening
/// session, and every other seat receives a separate invitation scope.
/// </remarks>
public sealed class RestrictedVisibilityPolicy(int authorizedSeat) : IVisibilityPolicy
{
    private readonly int authorizedSeat = authorizedSeat >= 0
        ? authorizedSeat
        : throw new ArgumentOutOfRangeException(nameof(authorizedSeat));

    /// <inheritdoc />
    public ViewScope Authorize(ViewerClaim? claim, int players)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(players);
        if (authorizedSeat >= players)
        {
            throw new InvalidOperationException(
                $"authorized seat {authorizedSeat} is outside a {players}-player game");
        }

        PermissiveVisibilityPolicy.ValidateClaim(claim, players);
        return new ViewScope([authorizedSeat]);
    }

    /// <inheritdoc />
    public IReadOnlyList<SeatScope> AdditionalScopes(ViewerClaim? claim, int players)
    {
        _ = Authorize(claim, players);
        return Enumerable.Range(0, players)
            .Where(seat => seat != authorizedSeat)
            .Select(seat => new SeatScope(seat, new ViewScope([seat])))
            .ToList();
    }
}
