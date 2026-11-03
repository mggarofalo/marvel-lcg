using System.Globalization;

namespace Marvel.Godot;

/// <summary>How the setup and join screens state their facts: an icon, a short label and a tooltip.</summary>
/// <remarks>
/// The screens show what is chosen and what is missing; the tooltips hold the explanation. Every
/// word here is our choice; none of it is a rule.
/// </remarks>
internal static class SetupWording
{
    /// <summary>The seed field's explanation, shown on hover instead of beneath the field.</summary>
    internal const string SeedTooltip = "Leave the seed blank for a fresh deal. The chosen seed stays visible for replay.";

    /// <summary>The invitation field's explanation, shown on hover instead of beneath the field.</summary>
    internal const string InvitationTooltip = "Paste the one-time seat invitation the host shared. It is cleared as soon as you join.";

    /// <summary>The deck library entry's short label.</summary>
    internal const string DeckLibrary = "Deck library";

    /// <summary>What the deck library entry opens.</summary>
    internal const string DeckLibraryTooltip = "Browse, build and import decks, then choose one for a seat.";

    /// <summary>One seat's deck: the seat number and the deck it plays, a published starter unless one was chosen.</summary>
    internal static Wording DeckSeat(int seat, string? deck) => new("your_seat",
        $"{Number(seat + 1)} · {deck ?? "Starter"}",
        $"Seat {Number(seat + 1)} plays {(deck is null ? "its hero's published starter deck" : "\"" + deck + "\"")}.");

    /// <summary>What a join needs, in the order the screen asks for it.</summary>
    internal static readonly IReadOnlyList<string> JoinInputs = ["Engine endpoint", "Game label", "Seat invitation"];

    /// <summary>The join readiness tracker's hover: what is still missing, or that the join can go ahead.</summary>
    internal static string JoinTooltip(IReadOnlyList<bool> provided)
    {
        string[] missing = [.. JoinInputs.Where((_, index) => !provided[index])];
        return missing.Length == 0 ? "Ready to join." : "Still needed: " + string.Join(", ", missing) + ".";
    }

    /// <summary>The host's one-time invitation for a seat; copying it removes the secret from the screen.</summary>
    internal static Wording Invitation(int seat) => new("your_seat", $"Seat {Number(seat + 1)} invitation",
        $"A one-time invitation for seat {Number(seat + 1)}. Copy it and send it to that player; "
        + "copying removes the secret from this screen.");

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
