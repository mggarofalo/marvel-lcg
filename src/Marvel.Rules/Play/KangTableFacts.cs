namespace Marvel.Rules.Play;

/// <summary>Public progress facts for The Once and Future Kang.</summary>
/// <param name="Stage">1 while Kang (I) stands, 2 while the table is separated, 3 once Kang (III) is in play.</param>
/// <param name="RemainingLocalKangs">Kang (II) villains in play that are not already departing.</param>
/// <param name="StoredDominions">Kang's Dominion cards stored facedown under stage 4A.</param>
/// <param name="CentralSchemes">Main schemes in no game area, such as stage 2B while the table is separated.</param>
/// <param name="PendingSplit">The defeated Kang (I) whose split waits for the end of the phase, if any.</param>
/// <param name="Departures">Kang (II) groups waiting to leave their game areas, by departing Kang id.</param>
/// <param name="Acting">The villain-phase step and seat resolving now, if any.</param>
public sealed record KangTableFacts(
    int Stage,
    int RemainingLocalKangs,
    int StoredDominions,
    IReadOnlyList<int> CentralSchemes,
    int? PendingSplit,
    IReadOnlyList<KangPendingDeparture> Departures,
    VillainPhaseFocus? Acting);
