using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Evaluates operands and publishes authorized information observations for domain execution.</summary>
internal static class AbilityDamageAndThreatQueries
{
    internal static long Amount(AbilityNumber number, AbilityDamageAndThreatContext context)
    {
        var selectors = new AbilitySelectorEvaluation(
            context.Expressions.Bindings, null, context.Program);
        var evaluation = new AbilityExpressionEvaluation(context.Expressions, selectors);
        var result = evaluation.Result(evaluation.Amount(number));
        Publish(result, context.World);
        return result.Value;
    }

    internal static Card? Find(AbilityCardSelection selection, AbilityDamageAndThreatContext context)
    {
        var evaluation = new AbilitySelectorEvaluation(
            context.Expressions.Bindings, null, context.Program);
        var result = evaluation.Result(evaluation.Find(selection));
        Publish(result, context.World);
        return result.Value;
    }

    internal static IReadOnlyList<Card> Every(AbilityCardSelection selection, AbilityDamageAndThreatContext context)
    {
        var evaluation = new AbilitySelectorEvaluation(
            context.Expressions.Bindings, null, context.Program);
        var result = evaluation.Result(evaluation.Every(selection));
        Publish(result, context.World);
        return result.Value;
    }

    internal static void Publish<T>(AbilityQueryResult<T> result, World world)
    {
        foreach (var observation in result.Information) world.RecordInformation(observation);
    }

    internal static int Resolver(AbilityDamageAndThreatContext context) =>
        AbilityCardQueries.Resolver(context.Expressions.Bindings);
}
