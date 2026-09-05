using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    // Only effect resolution consumes observations. Legality, projection and
    // prompt construction use the read-only evaluators without publishing.
    private static T PublishQuery<T>(AbilityQueryResult<T> result, Cast cast)
    {
        foreach (var observation in result.Information)
            cast.World.RecordInformation(observation);
        return result.Value;
    }

    private static long ResolveAmount(AbilityNumber number, Cast cast)
    {
        var evaluation = Expressions(cast);
        return PublishQuery(evaluation.Result(evaluation.Amount(number)), cast);
    }

    private static bool ResolveCondition(AbilityCondition condition, Cast cast) =>
        PublishQuery(EvaluateCondition(condition, cast), cast);

    private static Card? ResolveCard(AbilityCardSelection selector, Cast cast)
    {
        var evaluation = new AbilitySelectorEvaluation(cast.QueryContext());
        return PublishQuery(evaluation.Result(evaluation.Find(selector)), cast);
    }

    private static IReadOnlyList<Card> ResolveCards(AbilityCardSelection selector, Cast cast)
    {
        var evaluation = new AbilitySelectorEvaluation(cast.QueryContext());
        return PublishQuery(evaluation.Result(evaluation.Every(selector)), cast);
    }
}
