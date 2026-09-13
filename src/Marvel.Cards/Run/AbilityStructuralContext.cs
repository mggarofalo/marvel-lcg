using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

// One immutable read of a live resolution. The executor refreshes it after every
// command, so structural decisions never retain a stale view of the board.
internal sealed record AbilityStructuralContext(
    AbilityProgram Program,
    IResourceCardAbilities ResourceAbilities,
    IThreatCardAbilities ThreatAbilities,
    AbilityExpressionContext Expressions,
    AbilityReachabilityContext Reachability,
    string Trigger,
    string SourceFace,
    string AbilityFace,
    int Player,
    int Position,
    bool HasContinuation,
    AbilityType? Tier,
    string? Power,
    Card? AbilityActor,
    bool HasPendingDependency,
    ImmutableHashSet<AbilityEffect> CrisisIgnoringThwarts,
    ImmutableHashSet<int> PersistedCrisisIgnoringThwarts,
    ImmutableArray<AbilityStructuralFrame> Frames)
{
    internal AbilityAdmissionContext Admission() => new(
        Program, ResourceAbilities, Expressions, Reachability, Power, HasContinuation);
}
