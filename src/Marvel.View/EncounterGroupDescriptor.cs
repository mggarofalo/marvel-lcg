namespace Marvel.View;

/// <summary>Authorized public relationships for one independent villain and encounter deck.</summary>
public sealed record EncounterGroupDescriptor(
    int Group, string Title, int? Villain, int? Scheme, int Deck, int Discard)
{
    /// <summary>Whether this group currently carries the engine's active counter.</summary>
    public bool Active { get; init; }

    /// <summary>Whether every stage in this group has left the villain area after defeat.</summary>
    public bool Defeated { get; init; }

    /// <summary>Only readable physical stage identities, in engine-defined stage order.</summary>
    public IReadOnlyList<int> Stages { get; init; } = [];
}
