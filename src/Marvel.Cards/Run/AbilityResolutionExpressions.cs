using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionExpressions
{
    internal static bool BindingCanChange(this AbilityResolutionExecution execution, AbilityCondition condition) =>
        AbilityBindingAnalysis.BindingCanChange(condition);

    internal static bool BindingCanChange(this AbilityResolutionExecution execution, AbilityNumber number) =>
        AbilityBindingAnalysis.BindingCanChange(number);

    internal static bool BindingCanChange(this AbilityResolutionExecution execution, AbilityCardSelection selector) =>
        AbilityBindingAnalysis.BindingCanChange(selector);

    internal static bool AmountMayChange(this AbilityResolutionExecution execution, AbilityNumber number) =>
        AbilityBindingAnalysis.AmountMayChange(number);

    internal static bool ContainsPowerAmount(this AbilityResolutionExecution execution, AbilityNumber number) =>
        AbilityBindingAnalysis.ContainsPowerAmount(number);

    internal static bool ContainsPowerAmount(this AbilityResolutionExecution execution, AbilityCondition condition) =>
        AbilityBindingAnalysis.ContainsPowerAmount(condition);

    internal static bool WhenHolds(this AbilityResolutionExecution execution, CompiledCardAbility ability, AbilityResolutionState cast) =>
        ability.When is not { } condition || execution.Test(condition, cast);

    internal static bool ContainsYouOrYour(this AbilityResolutionExecution execution, AbilityNumber number) =>
        AbilityPlayerBindingAnalysis.Contains(number);

    internal static bool ContainsYouOrYour(this AbilityResolutionExecution execution, AbilityCondition condition) =>
        AbilityPlayerBindingAnalysis.Contains(condition);

    internal static AbilityExpressionEvaluation Expressions(this AbilityResolutionExecution execution, AbilityResolutionState cast)
    {
        var context = cast.ExpressionContext();
        return new AbilityExpressionEvaluation(
            context, new AbilitySelectorEvaluation(
                context.Bindings, execution.SingularAreaAdmission(cast), execution.program),
            execution.resourceAbilities);
    }

    internal static long Amount(this AbilityResolutionExecution execution, AbilityNumber number, AbilityResolutionState cast) =>
        execution.Expressions(cast).Amount(number);

    internal static bool Test(this AbilityResolutionExecution execution, AbilityCondition condition, AbilityResolutionState cast) =>
        execution.Expressions(cast).Test(condition);

    internal static int Seat(this AbilityResolutionExecution execution, AbilityPlayer player, AbilityResolutionState cast) =>
        execution.Expressions(cast).Seat(player);

    internal static AbilityQueryResult<bool> EvaluateCondition(this AbilityResolutionExecution execution, AbilityCondition condition, AbilityResolutionState cast)
    {
        var evaluation = execution.Expressions(cast);
        return evaluation.Result(evaluation.Test(condition));
    }
}
