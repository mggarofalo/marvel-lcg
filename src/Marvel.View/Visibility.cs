namespace Marvel.View;

/// <summary>What a client says it is displaying.</summary>
/// <param name="Seat">One seat, or null when the client did not name one.</param>
/// <param name="HotSeat">Whether one device is shared by the table.</param>
/// <param name="Watch">Whether the client asks to watch the table.</param>
/// <remarks>
/// This is a request, never authority. The server's <see cref="IVisibilityPolicy"/>
/// decides which of these seats the resulting session may actually see.
/// </remarks>
public sealed record ViewerClaim(int? Seat = null, bool HotSeat = false, bool Watch = false);

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
}

/// <summary>Chooses the private seats a newly opened server session may see.</summary>
public interface IVisibilityPolicy
{
    /// <summary>Authorizes a client assertion against server-owned policy.</summary>
    ViewScope Authorize(ViewerClaim? claim, int players);

    /// <summary>Returns separately scoped seats that the opener may invite.</summary>
    IReadOnlyList<SeatScope> AdditionalScopes(ViewerClaim? claim, int players);
}

/// <summary>A server-authorized seat and its private-information scope.</summary>
public sealed record SeatScope(int Seat, ViewScope Scope);

/// <summary>
/// Cooperative-table policy: a claimed seat sees itself, while hot-seat and
/// watch claims may see every player's private information.
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

        if (claim is null || claim.HotSeat || claim.Watch)
        {
            return new ViewScope(Enumerable.Range(0, players));
        }

        return claim.Seat is int seat ? new ViewScope([seat]) : ViewScope.None;
    }

    /// <inheritdoc />
    public IReadOnlyList<SeatScope> AdditionalScopes(ViewerClaim? claim, int players)
    {
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

/// <summary>Non-cooperative policy that binds the process to one authorized seat.</summary>
/// <remarks>
/// A client may under-claim and receive no private information. A <c>watch</c>,
/// <c>hot_seat</c>, or different-seat assertion cannot widen the seat configured
/// by the server operator.
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
        if (claim?.Seat is int claimed && claimed != authorizedSeat)
        {
            return ViewScope.None;
        }

        return new ViewScope([authorizedSeat]);
    }

    /// <inheritdoc />
    public IReadOnlyList<SeatScope> AdditionalScopes(ViewerClaim? claim, int players)
    {
        _ = Authorize(claim, players);
        if (claim?.Seat is int claimed && claimed != authorizedSeat)
        {
            return [];
        }

        return Enumerable.Range(0, players)
            .Where(seat => seat != authorizedSeat)
            .Select(seat => new SeatScope(seat, new ViewScope([seat])))
            .ToList();
    }
}
