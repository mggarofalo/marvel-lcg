using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// The output of initiation checks belongs to the enclosing ability resolution.
// Probes can establish a target exception without changing the board or another
// probe's assumptions. Persisted continuations copy these values by address.
internal sealed class AbilityInitiationEvidence
{
    internal bool LabelsPreflighted { get; set; }
    internal HashSet<AbilityEffect> CrisisIgnoringThwarts { get; } = new(ReferenceEqualityComparer.Instance);
    internal HashSet<int> PersistedCrisisIgnoringThwarts { get; } = [];
}
