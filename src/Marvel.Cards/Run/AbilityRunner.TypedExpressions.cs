using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
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

    private static bool WhenHolds(CompiledCardAbility ability, Cast cast) =>
        ability.When is not { } condition || Test(condition, cast);

    private static bool ContainsYouOrYour(AbilityNumber number) =>
        AbilityPlayerBindingAnalysis.Contains(number);

    private static bool ContainsYouOrYour(AbilityCondition condition) =>
        AbilityPlayerBindingAnalysis.Contains(condition);

    private static AbilityExpressionEvaluation Expressions(Cast cast)
    {
        var context = cast.ExpressionContext();
        return new AbilityExpressionEvaluation(
            context, new AbilitySelectorEvaluation(context.Bindings, SingularAreaAdmission(cast)));
    }

    private static long Amount(AbilityNumber number, Cast cast) =>
        Expressions(cast).Amount(number);

    private static bool Test(AbilityCondition condition, Cast cast) =>
        Expressions(cast).Test(condition);

    private static int Seat(AbilityPlayer player, Cast cast) =>
        Expressions(cast).Seat(player);

    private static AbilityQueryResult<bool> EvaluateCondition(AbilityCondition condition, Cast cast)
    {
        var evaluation = Expressions(cast);
        return evaluation.Result(evaluation.Test(condition));
    }
}
