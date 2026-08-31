using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>A client-safe snapshot of the table.</summary>
/// <param name="Players">Seats in seat order.</param>
/// <param name="Areas">Every runtime area in allocation order.</param>
/// <param name="GameAreas">The play-area groupings used by split scenarios.</param>
/// <param name="Outcome">How the game ended, or <see cref="Outcome.Unfinished"/>.</param>
public sealed record WorldDescriptor(
    IReadOnlyList<PlayerDescriptor> Players,
    IReadOnlyList<AreaDescriptor> Areas,
    IReadOnlyList<GameAreaDescriptor> GameAreas,
    Outcome Outcome);

/// <summary>Public information about one seat.</summary>
public sealed record PlayerDescriptor(int Seat, string Name, bool Eliminated);

/// <summary>One area and both card containers it owns.</summary>
/// <remarks>
/// The projector walks <see cref="World.Areas"/> rather than naming zones.
/// Therefore a newly allocated area is filtered on its first response without
/// adding it to a visibility list.
/// </remarks>
public sealed record AreaDescriptor(
    int Id,
    string Zone,
    int Owner,
    int Host,
    IReadOnlyList<CardDescriptor> Cards,
    IReadOnlyList<CardDescriptor> Removed);

/// <summary>One game-area grouping.</summary>
public sealed record GameAreaDescriptor(int Id, IReadOnlyList<int> PlayAreas);

/// <summary>The printed back a face-down card presents.</summary>
public enum CardBack
{
    /// <summary>A player-card back.</summary>
    Player,

    /// <summary>An encounter-card back.</summary>
    Encounter,
}

/// <summary>A card as one authorized client may see it.</summary>
/// <param name="Id">
/// The engine object id, or null while the card is concealed in a pile. A
/// concealed card has no stable wire identity, so a card seen before a shuffle
/// cannot be tracked through its new deck order. A physically face-down card
/// in play keeps its id so the client can still target it.
/// </param>
/// <param name="Back">The physical card back.</param>
/// <param name="FaceUp">Whether the face is physically up.</param>
/// <param name="Ready">Whether the card is ready.</param>
/// <param name="Host">The public object it is attached to, or -1.</param>
/// <param name="Face">Printed and live face information, or null when hidden.</param>
public sealed record CardDescriptor(
    int? Id,
    CardBack Back,
    bool FaceUp,
    bool Ready,
    int Host,
    CardFaceDescriptor? Face)
{
    /// <summary>The private audience used by the server-side filter.</summary>
    /// <remarks>Policy metadata is never serialized to the client.</remarks>
    internal CardAudience Audience { get; init; } = CardAudience.Nobody;

    /// <summary>Whether the physical card remains clickable while its face is hidden.</summary>
    internal bool Addressable { get; init; }
}

/// <summary>The readable face and its current state.</summary>
public sealed record CardFaceDescriptor(
    string Id,
    string Title,
    string Subtitle,
    CardKind Kind,
    IReadOnlyDictionary<string, long> Fields);

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
