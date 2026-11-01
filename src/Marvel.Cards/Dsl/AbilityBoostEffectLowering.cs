namespace Marvel.Cards.Dsl;

/// <summary>The fixed instructions that act on the boost icons or boost ability of a triggering card.</summary>
internal static class AbilityBoostEffectLowering
{
    /// <summary>Every operation word this lowering owns; each names <c>trigger</c>.</summary>
    internal static bool Owns(string kind) => kind is "cancelBoostAbility" or "cancelBoostIcons"
        or "increaseBoostCount" or "decreaseBoostCount" or "countDiscardedInstead";

    /// <summary>The instruction one owned operation word lowers to.</summary>
    internal static AbilityFixedInstruction Instruction(string kind) => kind switch
    {
        "cancelBoostAbility" => AbilityFixedInstruction.CancelBoostAbility,
        "cancelBoostIcons" => AbilityFixedInstruction.CancelBoostIcons,
        "increaseBoostCount" => AbilityFixedInstruction.IncreaseBoostCount,
        "decreaseBoostCount" => AbilityFixedInstruction.DecreaseBoostCount,
        "countDiscardedInstead" => AbilityFixedInstruction.CountDiscardedInstead,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "not a boost operation"),
    };
}
