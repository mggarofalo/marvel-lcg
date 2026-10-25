using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Collects singular area reads inside conditions, numbers, and selectors.</summary>
internal static class AbilityExpressionAreaDependencies
{
    internal static void CollectSingularAreaDependencies(
        AbilityCondition condition, AbilityAdmissionContext context, HashSet<DeckType> areas)
    {
        var cast = context;
        switch (condition)
        {
            case AbilityCondition.All all:
                foreach (var operand in all.Operands) CollectSingularAreaDependencies(operand, cast, areas);
                break;
            case AbilityCondition.Any any:
                foreach (var operand in any.Operands) CollectSingularAreaDependencies(operand, cast, areas);
                break;
            case AbilityCondition.Negated negated:
                CollectSingularAreaDependencies(negated.Operand, cast, areas);
                break;
            case AbilityCondition.AtLeast comparison:
                CollectSingularAreaDependencies(comparison.Value, cast, areas);
                CollectSingularAreaDependencies(comparison.Count, cast, areas);
                break;
            case AbilityCondition.CardText or AbilityCondition.IsKind
                or AbilityCondition.IsEnemyAttacker or AbilityCondition.WasDefeatedByDamage
                or AbilityCondition.WasDefeated:
                CollectCardsInDependencies(CardReadBy(condition), cast, areas);
                break;
            case AbilityCondition.Flag or AbilityCondition.PaidWithResource or AbilityCondition.DiscardedWithResource or AbilityCondition.DiscardedWithTrait
                or AbilityCondition.CausedThreat or AbilityCondition.Exists or AbilityCondition.LegalPractice
                or AbilityCondition.AutomaticThwart or AbilityCondition.TitleInPlay or AbilityCondition.InForm
                or AbilityCondition.ActivationIs or AbilityCondition.IsYourIdentity:
                break;
            default: throw new InvalidOperationException("Unknown compiled condition in area-dependency analysis");
        }
    }

    private static AbilityCardSelection CardReadBy(AbilityCondition condition) => condition switch
    {
        AbilityCondition.CardText text => text.Card,
        AbilityCondition.IsKind kind => kind.Card,
        AbilityCondition.IsEnemyAttacker attacker => attacker.Card,
        AbilityCondition.WasDefeatedByDamage defeated => defeated.Card,
        AbilityCondition.WasDefeated defeated => defeated.Card,
        _ => throw new InvalidOperationException("Condition does not read a card selector"),
    };

    internal static void CollectSingularAreaDependencies(
        AbilityNumber number, AbilityAdmissionContext context, HashSet<DeckType> areas)
    {
        var cast = context;
        switch (number)
        {
            case AbilityNumber.Sum sum:
                foreach (var operand in sum.Operands) CollectSingularAreaDependencies(operand, cast, areas);
                break;
            case AbilityNumber.Product product:
                foreach (var operand in product.Operands) CollectSingularAreaDependencies(operand, cast, areas);
                break;
            case AbilityNumber.FloorDivide division:
                foreach (var operand in division.Operands) CollectSingularAreaDependencies(operand, cast, areas);
                break;
            case AbilityNumber.Minimum minimum:
                foreach (var operand in minimum.Operands) CollectSingularAreaDependencies(operand, cast, areas);
                break;
            case AbilityNumber.Conditional conditional:
                CollectSingularAreaDependencies(conditional.Test, cast, areas);
                CollectSingularAreaDependencies(conditional.Then, cast, areas);
                CollectSingularAreaDependencies(conditional.Else, cast, areas);
                break;
            case AbilityNumber.SumPrintedCost sum:
                CollectCardsInDependencies(sum.Cards, cast, areas);
                break;
            case AbilityNumber.Constant or AbilityNumber.PerPlayer or AbilityNumber.Result
                or AbilityNumber.CardValue or AbilityNumber.Counters or AbilityNumber.Modified
                or AbilityNumber.Count or AbilityNumber.PrintedResourcesDiscarded
                or AbilityNumber.DiscardedWithResource or AbilityNumber.ResolutionValue:
                break;
            default: throw new InvalidOperationException("Unknown compiled number in area-dependency analysis");
        }
    }

    internal static void CollectCardsInDependencies(
        AbilityCardSelection selector, AbilityAdmissionContext context, HashSet<DeckType> areas) =>
        areas.UnionWith(AbilitySelectionAreaReads.For(selector));
}
