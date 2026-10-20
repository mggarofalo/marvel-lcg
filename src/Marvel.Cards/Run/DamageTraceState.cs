namespace Marvel.Cards.Run;

internal sealed record DamageTraceState(
        Dictionary<int, long> Damage, long PeakTargetPressure,
        HashSet<int> Players, Dictionary<int, int> Tough,
        HashSet<(int Card, string Status)> StatusChanges,
        Dictionary<(int Card, string Status), int> StatusCounts,
        Dictionary<int, long> Health, HashSet<int> Discarded,
        Dictionary<int, long> Threat, Dictionary<int, HashSet<string>> Traits,
        Dictionary<(int Card, string Field), long> Modifiers,
        Dictionary<int, int> Engagement,
        ulong FormsMayChange,
        int FirstPlayer,
        int CurrentVillain,
        int VillainStagesDrawn, bool Finished)
{
    internal Dictionary<int, int>? EncounterStages { get; init; }
    internal int[]? EncounterOrder { get; init; }
}
