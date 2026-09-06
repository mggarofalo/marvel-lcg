namespace Marvel.Cards.Run;

[Flags]
internal enum PowerReadiness
{
    Ready = 1,
    Exhausted = 2,
}

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
    int CurrentVillain, int VillainStagesDrawn, bool Finished);
