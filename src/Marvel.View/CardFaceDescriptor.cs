using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.View;

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
