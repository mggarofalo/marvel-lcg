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

        return Choose(seats, snapshot.Roles);
    }

    /// <summary>Records a user workspace choice when that seat is present in this snapshot.</summary>
    internal DisplayedSeatSelection Select(int seat, DisplayedSeatSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshot.SessionKey);
        int[] seats = OrderedSeats(snapshot.Seats);
        ResetForSessionChange(snapshot.SessionKey);
        selectedSeat = Contains(seats, seat) ? seat : null;
        return Choose(seats, snapshot.Roles);
    }

    private void ResetForSessionChange(string nextSessionKey)
    {
        if (sessionKey is not null
            && !string.Equals(sessionKey, nextSessionKey, StringComparison.Ordinal))
        {
            selectedSeat = null;
        }

        sessionKey = nextSessionKey;
    }

    private DisplayedSeatSelection Choose(
        int[] seats, DisplayedSeatRoles? roles)
    {
        DisplayedSeatRoles known = KnownRoles(roles, seats);
        return new DisplayedSeatSelection(
            ExpandedSeat(known, seats),
            known.ActivePlayer,
            known.PromptOwner,
            known.ViewedPrivateSeat,
            known.PublicFocusSeat);
    }

    private int ExpandedSeat(DisplayedSeatRoles roles, int[] seats)
    {
        // This is a desktop layout choice, not a rule: a seat-bound prompt takes the
        // workspace; otherwise a deliberate local choice takes precedence over the
        // host's public focus, then the active seat, then stable seat order.
        return roles.PromptOwner
            ?? selectedSeat
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
