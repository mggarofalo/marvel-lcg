namespace Marvel.View;

/// <summary>A visibility-reviewed connection between table subjects.</summary>
/// <remarks>
/// <paramref name="Subject"/> and <paramref name="Related"/> are emitted only when
/// both ids are in the authorized response. <paramref name="Seat"/> is used for an
/// engagement because a player is not a card object.
/// </remarks>
public sealed record TableRelationshipDescriptor(
    RelationshipKind Kind,
    int Subject,
    int? Related,
    int? Seat = null);
