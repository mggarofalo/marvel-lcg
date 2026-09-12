using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// The only mutable product of admission is scoped evidence that a continuation
// serializes by address. It is returned explicitly and never aliases AbilityResolutionState.
internal sealed record AbilityAdmissionResult(
    bool IsAdmissible,
    ImmutableHashSet<AbilityEffect> CrisisIgnoringThwarts)
{
    internal static AbilityAdmissionResult Rejected { get; } = new(false, []);
}
