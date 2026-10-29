using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>One game-owned execution frame, used unchanged by every suspension boundary.</summary>
internal sealed record AbilityContinuationState(
    AbilityInvocation Invocation, AbilityCursor Cursor, AbilityMemory Memory) : CardAbilityContinuation
{
    internal AbilityResumeReason Reason { get; init; } = AbilityResumeReason.Choice;
    internal AbilityDamageCompletion? DamageCompletion { get; init; }
    internal ThreatMoveReceipt? ThreatMove { get; init; }
    internal long ThreatMoveRequested { get; init; }
    internal System.Collections.Immutable.ImmutableArray<AbilityThreatMovePair> ThreatMovePairs { get; init; } = [];
    internal EffectCompletion? Completion { get; init; }
    internal AbilityStructuralOutcome? CompletedOutcome { get; init; }
    internal AbilityPowerExecutionState? PowerExecution { get; init; }
    internal AbilityPaymentPlan? Payment { get; init; }
    internal AllyEntryBinding? AllyEntryBinding { get; init; }
    internal DamageReplacement? DamageReplacement { get; init; }
}
