using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Marvel.Server")]

namespace Marvel.View;

/// <summary>
/// Cooperative-table policy in which every session sees the whole table.
/// </summary>
/// <remarks>
/// Permissiveness is an explicit server choice. It does not make the assertion
/// itself authority; replacing this policy changes the decision without
/// changing the wire request.
/// </remarks>
public sealed class PermissiveVisibilityPolicy : IVisibilityPolicy
{
    /// <inheritdoc />
    public ViewScope Authorize(ViewerClaim? claim, int players)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(players);
        ValidateClaim(claim, players);
        return new ViewScope(Enumerable.Range(0, players));
    }

    /// <inheritdoc />
    public IReadOnlyList<SeatScope> AdditionalScopes(ViewerClaim? claim, int players)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(players);
        ValidateClaim(claim, players);
        return [];
    }

    internal static void ValidateClaim(ViewerClaim? claim, int players)
    {
        if (claim is null)
        {
            return;
        }

        int modes = (claim.Seat.HasValue ? 1 : 0) + (claim.HotSeat ? 1 : 0) + (claim.Watch ? 1 : 0);
        if (modes > 1)
        {
            throw new ArgumentException("viewer must choose one of seat, hot_seat, or watch");
        }

        if (claim.Seat is int seat && (seat < 0 || seat >= players))
        {
            throw new ArgumentOutOfRangeException(nameof(claim), "viewer seat is outside this game");
        }
    }
}
