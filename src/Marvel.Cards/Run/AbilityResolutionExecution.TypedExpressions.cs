using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal sealed partial class AbilityResolutionExecution
{
    private static bool BindingCanChange(AbilityCondition condition) =>
        AbilityBindingAnalysis.BindingCanChange(condition);

    private static bool BindingCanChange(AbilityNumber number) =>
        AbilityBindingAnalysis.BindingCanChange(number);

    private static bool BindingCanChange(AbilityCardSelection selector) =>
        AbilityBindingAnalysis.BindingCanChange(selector);

    private static bool AmountMayChange(AbilityNumber number) =>
        AbilityBindingAnalysis.AmountMayChange(number);

    private static bool ContainsPowerAmount(AbilityNumber number) =>
        AbilityBindingAnalysis.ContainsPowerAmount(number);

    private static bool ContainsPowerAmount(AbilityCondition condition) =>
        AbilityBindingAnalysis.ContainsPowerAmount(condition);

    private bool WhenHolds(CompiledCardAbility ability, AbilityResolutionState cast) =>
        ability.When is not { } condition || Test(condition, cast);

    private static bool ContainsYouOrYour(AbilityNumber number) =>
        AbilityPlayerBindingAnalysis.Contains(number);

    private static bool ContainsYouOrYour(AbilityCondition condition) =>
        AbilityPlayerBindingAnalysis.Contains(condition);

    private AbilityExpressionEvaluation Expressions(AbilityResolutionState cast)
    {
        var context = cast.ExpressionContext();
        return new AbilityExpressionEvaluation(
            context, new AbilitySelectorEvaluation(
                context.Bindings, SingularAreaAdmission(cast), program),
            resourceAbilities);
    }

    private long Amount(AbilityNumber number, AbilityResolutionState cast) =>
        Expressions(cast).Amount(number);

    private bool Test(AbilityCondition condition, AbilityResolutionState cast) =>
        Expressions(cast).Test(condition);

    private int Seat(AbilityPlayer player, AbilityResolutionState cast) =>
        Expressions(cast).Seat(player);

    private AbilityQueryResult<bool> EvaluateCondition(AbilityCondition condition, AbilityResolutionState cast)
    {
        var evaluation = Expressions(cast);
        return evaluation.Result(evaluation.Test(condition));
    }
}
