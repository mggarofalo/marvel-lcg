using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Distinguishes receipt cardinality from reads that depend on which cards were discarded.</summary>
internal static class AbilityDiscardReceiptReads
{
    internal static bool NeedsPhysicalMembers(AbilityEffect effect) =>
        effect is AbilityEffect.RecoverDiscardedByResource or AbilityEffect.RecruitDiscarded
        || AbilityEffectStructure.EffectNumbers(effect).Any(NeedsPhysicalMembers)
        || AbilityEffectStructure.EffectConditions(effect).Any(NeedsPhysicalMembers)
        || AbilityEffectStructure.AllEffectChildren(effect).Any(NeedsPhysicalMembers);

    private static bool NeedsPhysicalMembers(AbilityNumber number) => number switch
    {
        AbilityNumber.PrintedResourcesDiscarded or AbilityNumber.DiscardedWithResource => true,
        AbilityNumber.ResolutionValue { Kind: AbilityResolutionNumber.BoostIconsDiscarded
            or AbilityResolutionNumber.PrintedResourceIconsDiscarded
            or AbilityResolutionNumber.PrintedCostDiscarded
            or AbilityResolutionNumber.DistinctCardTypesDiscarded
            or AbilityResolutionNumber.DistinctPrintedResourceTypesDiscarded } => true,
        AbilityNumber.Sum sum => sum.Operands.Any(NeedsPhysicalMembers),
        AbilityNumber.Product product => product.Operands.Any(NeedsPhysicalMembers),
        AbilityNumber.Minimum minimum => minimum.Operands.Any(NeedsPhysicalMembers),
        AbilityNumber.FloorDivide divide => divide.Operands.Any(NeedsPhysicalMembers),
        AbilityNumber.Conditional conditional => NeedsPhysicalMembers(conditional.Test)
            || NeedsPhysicalMembers(conditional.Then) || NeedsPhysicalMembers(conditional.Else),
        _ => false,
    };

    private static bool NeedsPhysicalMembers(AbilityCondition condition) => condition switch
    {
        AbilityCondition.DiscardedWithResource or AbilityCondition.DiscardedWithTrait => true,
        AbilityCondition.All all => all.Operands.Any(NeedsPhysicalMembers),
        AbilityCondition.Any any => any.Operands.Any(NeedsPhysicalMembers),
        AbilityCondition.Negated negated => NeedsPhysicalMembers(negated.Operand),
        AbilityCondition.AtLeast comparison => NeedsPhysicalMembers(comparison.Value)
            || NeedsPhysicalMembers(comparison.Count),
        _ => false,
    };
}
