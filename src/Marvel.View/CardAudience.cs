using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Who may read one card face.</summary>
internal readonly record struct CardAudience(bool Public, int Seat)
{
    /// <summary>Visible across the table.</summary>
    public static CardAudience Everyone { get; } = new(true, -1);

    /// <summary>Visible to no seat.</summary>
    public static CardAudience Nobody { get; } = new(false, -1);

    /// <summary>Visible only to one seat.</summary>
    public static CardAudience ForSeat(int seat) => new(false, seat);

    /// <summary>Whether this audience is visible through a scope.</summary>
    public bool IsVisible(ViewScope scope) => Public || (Seat >= 0 && scope.Includes(Seat));
}
