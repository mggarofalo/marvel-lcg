using System.Collections.Immutable;
using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

// Initiation evidence belongs to the enclosing resolution and survives its continuations.
internal sealed class AbilityInitiationEvidence
{
    internal bool LabelsPreflighted { get; set; }
    private HashSet<AbilityEffect> CrisisIgnoringThwarts { get; } = new(ReferenceEqualityComparer.Instance);
    private HashSet<int> PersistedCrisisIgnoringThwarts { get; } = [];
    internal ImmutableHashSet<AbilityEffect> ValidatedCrisisIgnoringThwarts =>
        ImmutableHashSet.CreateRange<AbilityEffect>(ReferenceEqualityComparer.Instance, CrisisIgnoringThwarts);
    internal ImmutableHashSet<int> RestoredCrisisIgnoringThwarts => PersistedCrisisIgnoringThwarts.ToImmutableHashSet();
    internal void ValidateCrisisIgnoringThwart(AbilityEffect node) => CrisisIgnoringThwarts.Add(node);
    internal bool CrisisIgnoringThwartWasValidated(AbilityEffect node, int ordinal) =>
        CrisisIgnoringThwarts.Contains(node) || PersistedCrisisIgnoringThwarts.Contains(ordinal);
    internal void RestoreCrisisIgnoringThwarts(IEnumerable<int> ordinals) => PersistedCrisisIgnoringThwarts.UnionWith(ordinals);
}
