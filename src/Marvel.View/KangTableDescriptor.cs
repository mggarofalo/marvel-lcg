namespace Marvel.View;

/// <summary>Public progress and comprehension facts for The Once and Future Kang.</summary>
/// <param name="Stage">1 while Kang (I) stands, 2 while the table is separated, 3 once Kang (III) is in play.</param>
/// <param name="RemainingLocalKangs">Kang (II) villains in play that are not already departing.</param>
/// <param name="StoredDominions">Kang's Dominion cards stored facedown under stage 4A; the count is public.</param>
/// <param name="CentralSchemes">Main schemes in no game area, such as stage 2B while the table is separated.</param>
/// <param name="PendingSplit">The defeated Kang (I) whose split waits for the end of the phase, if any.</param>
/// <param name="Departures">Kang (II) groups that leave their game areas at the end of the phase.</param>
public sealed record KangTableDescriptor(
    int Stage,
    int RemainingLocalKangs,
    int StoredDominions,
    IReadOnlyList<int> CentralSchemes,
    int? PendingSplit,
    IReadOnlyList<KangDepartureDescriptor> Departures)
{
    /// <summary>The villain-phase step and seat resolving now, if any.</summary>
    public VillainPhaseFocusDescriptor? Acting { get; init; }

    /// <summary>The number of players who started the scenario, which every per-player value counts.</summary>
    public int StartingPlayers { get; init; }

    /// <summary>The one encounter deck every game area shares, by area id.</summary>
    public int EncounterDeck { get; init; }

    /// <summary>The one encounter discard pile every game area shares, by area id.</summary>
    public int EncounterDiscard { get; init; }

    /// <summary>A readable sentence stating progress toward winning.</summary>
    public string Progress { get; init; } = "";

    /// <summary>The separated-table rules the engine says apply now, each keyed by kind.</summary>
    public IReadOnlyList<KangNoteDescriptor> Notes { get; init; } = [];
}
