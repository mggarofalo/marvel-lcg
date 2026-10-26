namespace Marvel.Cards.Run;

internal readonly record struct AbilityPowerState(
    ulong FormsMayChange, int FirstPlayer,
    long FirstPlayerDamage, bool FirstPlayerTough,
    Dictionary<int, long> CardDamage, Dictionary<int, bool> CardTough,
    HashSet<(int Card, string Status)> StatusChanges,
    Dictionary<(int Card, string Status), int> StatusCounts,
    Dictionary<int, PowerReadiness> CardReadiness, HashSet<int> Discarded,
    Dictionary<int, long> SchemeThreat,
    Dictionary<int, long> PlayerCardsAvailable,
    Dictionary<(int Card, string Field), long> Modifiers,
    Dictionary<int, HashSet<string>> Traits,
    Dictionary<int, int> Engagement,
    int CurrentVillain, int VillainStagesDrawn, bool Finished)
{
    // Only multi-villain scenarios need a projected current stage per immutable group.
    internal Dictionary<int, int>? EncounterStages { get; init; }
    internal int[]? EncounterOrder { get; init; }
    // Projection-only uncertainty, not a gameplay state or serialized field.
    // Prefix settlement is still checked; dependent reads must reject this state.
    internal string? UnprojectedStageEntry { get; init; }
    internal bool DefeatWindowDeferred { get; init; }
    internal bool RecoveryWindowDeferred { get; init; }
    internal int? RecoverySubject { get; init; }
    internal bool EncounterCardsChanged { get; init; }
    // Suspension admission alone may defer minion defeat for an independent
    // final tail. Full state projection never enables this permission.
    internal bool LiveMinionDefeatSettlement { get; init; }
}
