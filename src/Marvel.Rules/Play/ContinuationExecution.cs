namespace Marvel.Rules.Play;

/// <summary>Reads execution ownership without inspecting interpreter continuation data.</summary>
internal static class ContinuationExecution
{
    internal static AbilityExecution? Of(PhaseStep step) => step.Operation switch
    {
        NativeRetaliationOperation operation => operation.Resolution.Execution,
        SimultaneousAttackDamageOperation operation => operation.Resolution.Execution,
        BasicAttackDivisionOperation operation => operation.Resolution.Execution,
        AttackDamageResolutionOperation operation => operation.Resolution.Execution,
        AbilityContinuationAgendaOperation operation => operation.Continuation?.Execution,
        CharacterAttackOperation operation => operation.Attack.Continuation?.Execution,
        CharacterThwartOperation operation => operation.Thwart.Continuation?.Execution,
        EventCardCompletionOperation operation => operation.Execution,
        CardPaymentEntryOperation operation => operation.Entry.Execution,
        ResourcePaymentOperation operation => operation.Payment.Execution,
        SimultaneousDirectDamageOperation operation => operation.Resolution.Execution,
        DamageResolutionOperation operation => operation.Resolution.Execution,
        DamageCostPlacementOperation operation => operation.Placement.Execution,
        _ => null,
    };
}
