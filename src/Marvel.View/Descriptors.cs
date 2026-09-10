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

/// <summary>The readable printed or effective identity and its current state.</summary>
/// <param name="Id">
/// A printed face id, or a stable presentation id when an effect replaces the visible
/// gameplay identity. Only <see cref="ArtFaceId"/> may be used to request card art.
/// </param>
/// <param name="Title">The visible printed or effective title.</param>
/// <param name="Subtitle">The visible subtitle.</param>
/// <param name="Kind">The card's effective kind.</param>
/// <param name="Fields">Its current visibility-safe gameplay fields.</param>
public sealed record CardFaceDescriptor(
    string Id,
    string Title,
    string Subtitle,
    CardKind Kind,
    IReadOnlyDictionary<string, long> Fields)
{
    /// <summary>The printed traits, in catalog order.</summary>
    public IReadOnlyList<string> Traits { get; init; } = [];

    /// <summary>The printed cost, or null when the face has no cost.</summary>
    public string? Cost { get; init; }

    /// <summary>The printed attributes used as card stats, without invented defaults.</summary>
    public IReadOnlyDictionary<string, string> PrintedStats { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>The printed keyword labels, in catalog order.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = [];

    /// <summary>The printed rules text.</summary>
    public string RulesText { get; init; } = string.Empty;

    /// <summary>Printed rules text with the source's display-only emphasis and symbols.</summary>
    /// <remarks>
    /// This additive protocol field is a product choice. It carries no executable
    /// rules meaning; clients render only the formatting vocabulary they support.
    /// </remarks>
    public string RulesMarkup { get; init; } = string.Empty;

    /// <summary>The visible printed face id a client may use for optional local art.</summary>
    /// <remarks>
    /// This is distinct from <see cref="Id"/> because an effective public identity,
    /// such as a facedown Drone, must never reveal or request its underlying art.
    /// </remarks>
    public string? ArtFaceId { get; init; }

    /// <summary>Damage currently on the card.</summary>
    public long Damage { get; init; }

    /// <summary>Live counters currently on the card, by semantic counter name.</summary>
    public IReadOnlyDictionary<string, long> Counters { get; init; } =
        new Dictionary<string, long>(StringComparer.Ordinal);
}

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
