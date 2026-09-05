using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// Assumptions for one speculative path, not state produced by resolving it.
// A child probe receives a new snapshot; there is no mode to restore afterward.
internal sealed record AbilityReachabilityContext
{
    internal bool CheckingInitiation { get; init; }
    internal bool FilteringContinuationOption { get; init; }
    internal bool PaymentMayMutate { get; init; }
    internal AbilityCost? PaymentCost { get; init; }
    internal ImmutableList<AbilityEffect> PriorSteps { get; init; } = [];
    internal bool PriorStepMayMutate { get; init; }
    internal ulong PriorFormsMayChange { get; init; }
    internal bool PriorBindingMayChange { get; init; }
    internal ImmutableList<Card> PriorBindingCandidates { get; init; } = [];
    internal bool PriorBindingMayBeEmpty { get; init; }
}

// The output of initiation checks belongs to the enclosing ability resolution.
// Probes can establish a target exception without changing the board or another
// probe's assumptions. Persisted continuations copy these values by address.
internal sealed class AbilityInitiationEvidence
{
    internal bool LabelsPreflighted { get; set; }
    internal HashSet<AbilityEffect> CrisisIgnoringThwarts { get; } = new(ReferenceEqualityComparer.Instance);
    internal HashSet<int> PersistedCrisisIgnoringThwarts { get; } = [];
}
