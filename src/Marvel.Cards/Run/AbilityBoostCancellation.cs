using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Executes independent star and numeric cancellation against exact boost receipts.</summary>
internal static class AbilityBoostCancellation
{
    internal static AbilityImmediateResult? TryRun(AbilityEffect.Fixed effect, AbilityImmediateContext context)
    {
        var occurrence = context.Admission.Query.Occurrence;
        switch (effect.Instruction)
        {
            case AbilityFixedInstruction.CancelBoostAbility:
                bool cancelled = (occurrence.BoostAbility
                    ?? throw new RulesNotImplementedException("boost ability cancellation requires its exact faceup occurrence"))
                    .Cancel(context.Admission.World);
                return new(true, cancelled);
            case AbilityFixedInstruction.CancelBoostIcons:
                long amount = (occurrence.BoostIcons
                    ?? throw new RulesNotImplementedException("boost cancellation requires a faceup card occurrence"))
                    .Cancel(context.Admission.World);
                return new(true, amount > 0) { CancelledBoostIcons = amount };
            default:
                return null;
        }
    }
}
