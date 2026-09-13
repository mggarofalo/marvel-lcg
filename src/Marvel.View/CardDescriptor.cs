using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.View;

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
    /// <summary>Public placement and controller information, or null for an anonymous concealed pile entry.</summary>
    public CardLocationDescriptor? Location { get; init; }

    /// <summary>Public live values, or null when the card face is concealed.</summary>
    public CardStateDescriptor? State { get; init; }

    /// <summary>The private audience used by the server-side filter.</summary>
    /// <remarks>Policy metadata is never serialized to the client.</remarks>
    internal CardAudience Audience { get; init; } = CardAudience.Nobody;

    /// <summary>Whether the physical card remains clickable while its face is hidden.</summary>
    internal bool Addressable { get; init; }
}
