namespace Marvel.Cards.Run;

// Change evidence for the same projected board transition. Missing numeric entries
// retain their live values; departure invalidates every observation of that card.
internal sealed class VillainTraceChanges(
    HashSet<int> discarded,
    IReadOnlyDictionary<int, long> threat,
    IReadOnlyDictionary<int, long> damage,
    IReadOnlyDictionary<(int Card, string Field), long> modifiers,
    IReadOnlyDictionary<int, HashSet<string>> traits,
    IReadOnlySet<(int Card, string Status)> statuses,
    IReadOnlyDictionary<int, int> engagement)
{
    internal HashSet<int> Discarded { get; } = discarded;
    internal IReadOnlyDictionary<int, long> Threat { get; } = threat;
    internal IReadOnlyDictionary<int, long> Damage { get; } = damage;
    internal IReadOnlyDictionary<(int Card, string Field), long> Modifiers { get; } = modifiers;
    internal IReadOnlyDictionary<int, HashSet<string>> Traits { get; } = traits;
    internal IReadOnlySet<(int Card, string Status)> Statuses { get; } = statuses;
    internal IReadOnlyDictionary<int, int> Engagement { get; } = engagement;

    internal bool ThreatMayDiffer(int card) =>
        Discarded.Contains(card) || Threat.ContainsKey(card);

    internal bool DamageMayDiffer(int card) =>
        Discarded.Contains(card) || Damage.ContainsKey(card);

    internal bool HealthMayDiffer(int card) =>
        DamageMayDiffer(card) || Modifiers.ContainsKey((card, "health"));

    internal bool FieldMayDiffer(int card, string field) =>
        DamageMayDiffer(card)
        || Modifiers.ContainsKey((card, field))
        || Traits.ContainsKey(card)
        || Statuses.Any(change => change.Card == card);

    internal bool StatusMayDiffer(int card, string status) =>
        Discarded.Contains(card) || Statuses.Contains((card, status));

    internal bool TraitMayDiffer(int card, string trait) =>
        Discarded.Contains(card)
        || Traits.TryGetValue(card, out var changed) && changed.Contains(trait);
}
