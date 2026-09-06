using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

internal static partial class AbilityBindingAnalysis
{
    internal static bool BindingCanChange(AbilityCondition condition) => condition switch
    {
        AbilityCondition.All all => all.Operands.Any(BindingCanChange),
        AbilityCondition.Any any => any.Operands.Any(BindingCanChange),
        AbilityCondition.Negated negated => BindingCanChange(negated.Operand),
        AbilityCondition.Exists exists => BindingCanChange(exists.Cards),
        AbilityCondition.LegalPractice practice => BindingCanChange(practice.Schemes),
        AbilityCondition.AutomaticThwart thwart => BindingCanChange(thwart.Scheme),
        AbilityCondition.AtLeast comparison => BindingCanChange(comparison.Value)
            || BindingCanChange(comparison.Count),
        AbilityCondition.InForm form => form.Player == AbilityPlayer.ChosenPlayer,
        AbilityCondition.CardText text => BindingCanChange(text.Card),
        AbilityCondition.IsKind kind => BindingCanChange(kind.Card),
        AbilityCondition.WasDefeated defeated => BindingCanChange(defeated.Card),
        AbilityCondition.IsYourIdentity identity => BindingCanChange(identity.Card),
        AbilityCondition.Flag or AbilityCondition.PaidWithResource or AbilityCondition.DiscardedWithResource
            or AbilityCondition.CausedThreat or AbilityCondition.TitleInPlay or AbilityCondition.ActivationIs => false,
        _ => throw new InvalidOperationException("Unknown compiled condition in binding analysis"),
    };

    internal static bool BindingCanChange(AbilityNumber number) => number switch
    {
        AbilityNumber.Sum sum => sum.Operands.Any(BindingCanChange),
        AbilityNumber.Product product => product.Operands.Any(BindingCanChange),
        AbilityNumber.Minimum minimum => minimum.Operands.Any(BindingCanChange),
        AbilityNumber.CardValue value => BindingCanChange(value.Card),
        AbilityNumber.Counters counters => BindingCanChange(counters.Card),
        AbilityNumber.Modified modified => BindingCanChange(modified.Card),
        AbilityNumber.Count count => BindingCanChange(count.Cards),
        AbilityNumber.Conditional conditional => BindingCanChange(conditional.Test)
            || BindingCanChange(conditional.Then) || BindingCanChange(conditional.Else),
        AbilityNumber.ResolutionValue value => value.Kind == AbilityResolutionNumber.PowerAmount,
        AbilityNumber.Constant or AbilityNumber.PerPlayer or AbilityNumber.Result
            or AbilityNumber.PrintedResourcesDiscarded or AbilityNumber.DiscardedWithResource => false,
        _ => throw new InvalidOperationException("Unknown compiled number in binding analysis"),
    };

    internal static bool BindingCanChange(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.Bound bound => bound.Binding is AbilityCardBinding.Chosen or AbilityCardBinding.That,
        AbilityCardSelection.Query query => query.Kind is AbilityCardQuery.PowerTargets
            or AbilityCardQuery.EnemiesEngagedWithChosenPlayer or AbilityCardQuery.TopmostTechInChosenDiscard,
        AbilityCardSelection.WithTrait trait => BindingCanChange(trait.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached other => BindingCanChange(other.Cards),
        AbilityCardSelection.Discardable discardable => BindingCanChange(discardable.Cards),
        AbilityCardSelection.Ranked ranked => BindingCanChange(ranked.Cards),
        AbilityCardSelection.Titled or AbilityCardSelection.EnemiesWithTrait or AbilityCardSelection.InAreas => false,
        _ => throw new InvalidOperationException("Unknown compiled selector in binding analysis"),
    };

    internal static bool AmountMayChange(AbilityNumber number) => number switch
    {
        AbilityNumber.Constant or AbilityNumber.PerPlayer => false,
        AbilityNumber.ResolutionValue { Kind: AbilityResolutionNumber.PowerAmount } => false,
        AbilityNumber.Sum sum => sum.Operands.Any(AmountMayChange),
        AbilityNumber.Product product => product.Operands.Any(AmountMayChange),
        AbilityNumber.Minimum minimum => minimum.Operands.Any(AmountMayChange),
        AbilityNumber.Result or AbilityNumber.CardValue or AbilityNumber.Counters
            or AbilityNumber.Modified or AbilityNumber.Count or AbilityNumber.Conditional
            or AbilityNumber.PrintedResourcesDiscarded or AbilityNumber.DiscardedWithResource
            or AbilityNumber.ResolutionValue => true,
        _ => throw new InvalidOperationException("Unknown compiled number in mutation analysis"),
    };

    internal static bool ContainsPowerAmount(AbilityNumber number) => number switch
    {
        AbilityNumber.ResolutionValue value => value.Kind == AbilityResolutionNumber.PowerAmount,
        AbilityNumber.Sum sum => sum.Operands.Any(ContainsPowerAmount),
        AbilityNumber.Product product => product.Operands.Any(ContainsPowerAmount),
        AbilityNumber.Minimum minimum => minimum.Operands.Any(ContainsPowerAmount),
        AbilityNumber.Conditional conditional => ContainsPowerAmount(conditional.Test)
            || ContainsPowerAmount(conditional.Then) || ContainsPowerAmount(conditional.Else),
        AbilityNumber.Constant or AbilityNumber.PerPlayer or AbilityNumber.Result
            or AbilityNumber.CardValue or AbilityNumber.Counters or AbilityNumber.Modified
            or AbilityNumber.Count or AbilityNumber.PrintedResourcesDiscarded
            or AbilityNumber.DiscardedWithResource => false,
        _ => throw new InvalidOperationException("Unknown compiled number in power-binding analysis"),
    };

    internal static bool ContainsPowerAmount(AbilityCondition condition) => condition switch
    {
        AbilityCondition.All all => all.Operands.Any(ContainsPowerAmount),
        AbilityCondition.Any any => any.Operands.Any(ContainsPowerAmount),
        AbilityCondition.Negated negated => ContainsPowerAmount(negated.Operand),
        AbilityCondition.AtLeast comparison => ContainsPowerAmount(comparison.Value)
            || ContainsPowerAmount(comparison.Count),
        AbilityCondition.Flag or AbilityCondition.PaidWithResource or AbilityCondition.DiscardedWithResource
            or AbilityCondition.CausedThreat or AbilityCondition.Exists or AbilityCondition.LegalPractice
            or AbilityCondition.AutomaticThwart or AbilityCondition.TitleInPlay or AbilityCondition.InForm
            or AbilityCondition.ActivationIs or AbilityCondition.CardText or AbilityCondition.IsKind
            or AbilityCondition.WasDefeated or AbilityCondition.IsYourIdentity => false,
        _ => throw new InvalidOperationException("Unknown compiled condition in power-binding analysis"),
    };

}
