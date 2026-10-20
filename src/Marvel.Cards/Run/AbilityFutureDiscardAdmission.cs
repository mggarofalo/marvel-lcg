using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityAdmission;

namespace Marvel.Cards.Run;

/// <summary>Recognizes a discard receipt that an earlier effect has not produced yet.</summary>
internal static class AbilityFutureDiscardAdmission
{
    internal static bool MayDealDamage(AbilityNumber number, AbilityAdmissionScope scope) =>
        AbilityInitiationPrimitives.Amount(number, scope) > 0 || HasPendingAmount(number, scope);

    private static bool HasPendingAmount(AbilityNumber number, AbilityAdmissionScope scope) =>
        UsesDiscardReceipt(number) && scope.Reachability.PriorSteps.Any(effect =>
            effect is AbilityEffect.DiscardTop discard
                && AbilityInitiationPrimitives.Amount(discard.Count, scope) > 0);

    private static bool UsesDiscardReceipt(AbilityNumber number) => number switch
    {
        AbilityNumber.ResolutionValue { Kind: AbilityResolutionNumber.PrintedBoostIconsDiscarded
            or AbilityResolutionNumber.PrintedResourceIconsDiscarded
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

