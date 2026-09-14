namespace Marvel.Godot;

/// <summary>Retains the local public-workspace choice across stable table snapshots.</summary>
/// <remarks>
/// The host supplies seat roles after visibility filtering. This collaborator chooses only
/// layout focus; it neither grants private visibility nor determines which interaction is legal.
/// </remarks>
internal sealed class DisplayedSeatState
{
    private string? sessionKey;
    private int? selectedSeat;
    private int? focusedSeat;

    /// <summary>Reconciles a new authoritative snapshot without treating a revision as a new session.</summary>
    internal DisplayedSeatSelection Update(DisplayedSeatSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshot.SessionKey);
        int[] seats = OrderedSeats(snapshot.Seats);
        ResetForSessionChange(snapshot.SessionKey);
        if (!Contains(seats, selectedSeat))
        {
            selectedSeat = null;
        }

        int? focus = Contains(seats, focusedSeat) ? focusedSeat : null;
        focusedSeat = null;
        return Choose(seats, snapshot.Roles, focus);
    }

    /// <summary>Records a user workspace choice when that seat is present in this snapshot.</summary>
    internal DisplayedSeatSelection Select(int seat, DisplayedSeatSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshot.SessionKey);
        int[] seats = OrderedSeats(snapshot.Seats);
        ResetForSessionChange(snapshot.SessionKey);
        selectedSeat = Contains(seats, seat) ? seat : null;
        focusedSeat = null;
        return Choose(seats, snapshot.Roles);
    }

    /// <summary>Places a prompt or event anchor in view for the next board render only.</summary>
    internal void Focus(int seat, DisplayedSeatSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshot.SessionKey);
        int[] seats = OrderedSeats(snapshot.Seats);
        ResetForSessionChange(snapshot.SessionKey);
        focusedSeat = Contains(seats, seat) ? seat : null;
    }

    private void ResetForSessionChange(string nextSessionKey)
    {
        if (sessionKey is not null
            && !string.Equals(sessionKey, nextSessionKey, StringComparison.Ordinal))
        {
            selectedSeat = null;
            focusedSeat = null;
        }

        sessionKey = nextSessionKey;
    }

    private DisplayedSeatSelection Choose(
        int[] seats,
        DisplayedSeatRoles? roles,
        int? focus = null)
    {
        DisplayedSeatRoles known = KnownRoles(roles, seats);
        return new DisplayedSeatSelection(
            ExpandedSeat(known, seats, focus),
            known.ActivePlayer,
            known.PromptOwner,
            known.ViewedPrivateSeat,
            known.PublicFocusSeat);
    }

    private int ExpandedSeat(DisplayedSeatRoles roles, int[] seats, int? focus)
    {
        // This is a desktop layout choice, not a rule: an anchor is shown for
        // one render, while a deliberate local choice remains visible while its
        // seat exists. Otherwise a seat-bound prompt opens its owner, followed
        // by the host's public focus, the active seat, and stable seat order.
        return focus
            ?? selectedSeat
            ?? roles.PromptOwner
            ?? roles.PublicFocusSeat
            ?? roles.ActivePlayer
            ?? seats[0];
    }

    private static int[] OrderedSeats(IReadOnlyList<int> seats)
    {
        ArgumentNullException.ThrowIfNull(seats);
        int[] ordered = [.. seats.Distinct().OrderBy(seat => seat)];
        if (ordered.Length == 0)
        {
            throw new ArgumentException("A displayed tabletop requires at least one player seat.", nameof(seats));
        }

        return ordered;
    }

    private static bool Contains(int[] seats, int? seat) => seat is { } value
        && Contains(seats, value);

    private static bool Contains(int[] seats, int seat) => seats.Contains(seat);

    private static DisplayedSeatRoles KnownRoles(DisplayedSeatRoles? roles, int[] seats) => new(
        KnownSeat(roles?.PromptOwner, seats),
        KnownSeat(roles?.ViewedPrivateSeat, seats),
        KnownSeat(roles?.ActivePlayer, seats),
        KnownSeat(roles?.PublicFocusSeat, seats));

    private static int? KnownSeat(int? seat, int[] seats) => Contains(seats, seat)
        ? seat
        : null;
}
