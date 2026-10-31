namespace Marvel.View;

/// <summary>Stable public placement and engine-authoritative control information for an addressable card.</summary>
public sealed record CardLocationDescriptor(
    int AreaId,
    string Zone,
    int Controller,
    int EngagedWith)
{
    /// <summary>
    /// The id of the game area the engine places this card in, or null when it is in
    /// none, such as Kang's central stage 2B. A card's game area usually follows its
    /// area's play area; the engine's answer also covers cards whose association
    /// outlives their play area's membership.
    /// </summary>
    public int? GameArea { get; init; }
}
