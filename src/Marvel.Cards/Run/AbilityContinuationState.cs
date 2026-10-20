using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>One game-owned execution frame, used unchanged by every suspension boundary.</summary>
internal sealed record AbilityContinuationState(
    AbilityInvocation Invocation, AbilityCursor Cursor, AbilityMemory Memory) : CardAbilityContinuation
{
    internal AbilityResumeReason Reason { get; init; } = AbilityResumeReason.Choice;
    internal AbilityDamageCompletion? DamageCompletion { get; init; }
    internal EffectCompletion? Completion { get; init; }
    internal AbilityStructuralOutcome? CompletedOutcome { get; init; }
    internal AbilityPowerExecutionState? PowerExecution { get; init; }
    internal AbilityPaymentPlan? Payment { get; init; }
    internal DamageReplacement? DamageReplacement { get; init; }
}
