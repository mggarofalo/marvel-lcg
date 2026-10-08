using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Evaluates structural expressions and publishes their information observations.</summary>
internal static class AbilityStructuralEvaluation
{
    internal static long Amount(AbilityNumber number, AbilityStructuralContext context)
    {
        var evaluation = Evaluation(context);
        return Publish(evaluation.Result(evaluation.Amount(number)), context.Expressions.World);
    }

    internal static bool Test(AbilityCondition condition, AbilityStructuralContext context)
    {
        var evaluation = Evaluation(context);
        return Publish(evaluation.Result(evaluation.Test(condition)), context.Expressions.World);
    }

    internal static AbilityExpressionEvaluation Evaluation(AbilityStructuralContext context) =>
        new(context.Expressions, new AbilitySelectorEvaluation(context.Expressions.Bindings));

    internal static IReadOnlyList<Card> Every(AbilityCardSelection selection, AbilityStructuralContext context)
    {
        var evaluation = new AbilitySelectorEvaluation(
            context.Expressions.Bindings, program: context.Program);
        return Publish(evaluation.Result(evaluation.Every(selection)), context.Expressions.World);
    }

    internal static Card? Find(
        AbilityCardSelection selection, AbilityStructuralContext context)
    {
        var evaluation = new AbilitySelectorEvaluation(
            context.Expressions.Bindings, program: context.Program);
        return Publish(evaluation.Result(evaluation.Find(selection)), context.Expressions.World);
    }

    internal static List<Card> TopCards(Area deck, long count) =>
        [.. deck.Cards.TakeLast(checked((int)Math.Max(0, count))).Reverse()];

    internal static T Publish<T>(AbilityQueryResult<T> result, World world)
    {
        foreach (var observation in result.Information)
            world.RecordInformation(observation);
        return result.Value;
    }
}
