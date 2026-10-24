using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Preserves single-card selection semantics in a projected encounter topology.</summary>
internal static class AbilitySingularTraceSelection
{
    internal static Card? Find(AbilityCardSelection selector, AbilitySelectorTraceContext trace)
    {
        if (selector is AbilityCardSelection.FirstAvailable fallback)
        {
            foreach (var option in fallback.Options)
                if (AbilitySelectorTrace.MatchingCandidates(option, trace).Any())
                    return Find(option, trace);
            return null;
        }
        if (!Supports(selector))
            throw new RulesNotImplementedException(
                $"'{trace.Cast.Source.FaceId}' uses a projected selector with no implemented single-card resolution");
        var found = AbilitySelectorTrace.MatchingCandidates(selector, trace).Take(2).ToArray();
        return found.Length switch
        {
            0 => null,
            1 => found[0],
            _ => throw new RulesNotImplementedException(
                $"'{trace.Cast.Source.FaceId}' has a projected selector matching multiple cards where one card is required"),
        };
    }

    private static bool Supports(AbilityCardSelection selector) => selector is
        AbilityCardSelection.Bound or AbilityCardSelection.Titled or AbilityCardSelection.Last
        or AbilityCardSelection.InAreas or AbilityCardSelection.Query
        {
            Kind: AbilityCardQuery.Villain or AbilityCardQuery.MainScheme or AbilityCardQuery.ActiveVillainSideScheme
                or AbilityCardQuery.YourAsideMinion or AbilityCardQuery.YourAsideSideScheme,
        };
}
