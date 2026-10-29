using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityAdmission;

namespace Marvel.Cards.Run;

/// <summary>Recognizes a discard receipt that a cost or earlier effect has not produced yet.</summary>
internal static class AbilityFutureDiscardAdmission
{
    internal static bool MayHavePositiveAmount(AbilityNumber number, AbilityAdmissionScope scope) =>
        AbilityInitiationPrimitives.Amount(number, scope) > 0 || HasPendingAmount(number, scope);

    private static bool HasPendingAmount(AbilityNumber number, AbilityAdmissionScope scope) =>
        UsesDiscardReceipt(number) && !HasFixedZeroBound(number)
        && (scope.Reachability.PriorSteps.Any(effect => ProducesReceipt(effect, scope))
            || scope.Reachability.PaymentMayMutate && ProducesReceipt(scope.Reachability.PaymentCost));

    private static bool ProducesReceipt(AbilityCost? cost) => cost switch
    {
        AbilityCost.DiscardTop { Count: > 0 } => true,
        AbilityCost.Sequence sequence => sequence.Costs.Any(ProducesReceipt),
        _ => false,
    };

    private static bool HasFixedZeroBound(AbilityNumber number) => number switch
    {
        AbilityNumber.Minimum minimum => minimum.Operands.Any(operand => operand is AbilityNumber.Constant { Value: <= 0 }),
        AbilityNumber.Product product => product.Operands.Any(operand => operand is AbilityNumber.Constant { Value: 0 }),
        _ => false,
    };

    private static bool ProducesReceipt(AbilityEffect effect, AbilityAdmissionScope scope) => effect switch
    {
        AbilityEffect.DiscardTop discard => AbilityInitiationPrimitives.Amount(discard.Count, scope) > 0,
        AbilityEffect.DiscardAtRandom discard => AbilityInitiationPrimitives.Amount(discard.Count, scope) > 0,
        _ => false,
    };

    private static bool UsesDiscardReceipt(AbilityNumber number) => number switch
    {
        AbilityNumber.ResolutionValue { Kind: AbilityResolutionNumber.CardsDiscarded
            or AbilityResolutionNumber.PrintedBoostIconsDiscarded
            or AbilityResolutionNumber.PrintedResourceIconsDiscarded
            or AbilityResolutionNumber.PrintedCostDiscarded
            or AbilityResolutionNumber.DistinctCardTypesDiscarded
            or AbilityResolutionNumber.DistinctPrintedResourceTypesDiscarded
            or AbilityResolutionNumber.TopEncounterDiscardBoostPlusOne } => true,
        AbilityNumber.PrintedResourcesDiscarded or AbilityNumber.DiscardedWithResource => true,
        AbilityNumber.Sum sum => sum.Operands.Any(UsesDiscardReceipt),
        AbilityNumber.Product product => product.Operands.Any(UsesDiscardReceipt),
        AbilityNumber.Minimum minimum => minimum.Operands.Any(UsesDiscardReceipt),
        AbilityNumber.FloorDivide division => division.Operands.Any(UsesDiscardReceipt),
        _ => false,
    };
}

