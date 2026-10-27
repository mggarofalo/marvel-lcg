using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Completed values and exact card bindings retained across a suspension.</summary>
internal sealed record AbilityMemory(ImmutableDictionary<string, long> Results,
    ImmutableArray<int> Discarded, AbilityContinuationCardBinding? Chosen,
    ImmutableHashSet<int> CrisisIgnoringThwarts, CardInstanceState SourceState,
    bool SurgeGained)
{
    internal System.Collections.Immutable.ImmutableArray<(int Card, int Incarnation)> DamageDefeats { get; init; } = [];
    internal ImmutableHashSet<int> ExhaustedForCost { get; init; } = [];
    // Engine-owned provenance of retained quantities, independent of the card's current zone.
    internal bool SourceWasPublicInPlay { get; init; }
}
