using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal static class AbilitySelectedHandDiscardPreflight
{
    internal static void Validate(AbilityStructuralContext context, AbilityEffect choice,
        AbilityContinuationFacts continuation)
    {
        if (!continuation.HasPath) return;
        var admission = context.Admission();
        var remaining = AbilityStructuralQueries.Remaining(
            context, continuation, admission, null, AbilityStructuralQueries.NewEvidence());
        var sensitive = new HashSet<DeckType>();
        foreach (var step in remaining)
            AbilityAdmissionAreaDependencies.Collect(step, admission, sensitive);
        // Repository choice: refuse unprojected singular area reads before the
        // selected cards move. Ordinary receipt-based counts remain supported.
        if (sensitive.Count > 0
            && AbilityRuntimeQueries.EffectsMayChangeAnyArea([choice], sensitive, admission))
            throw new RulesNotImplementedException(
                $"'{context.SourceFace}' changes a singular area query before its continuation");
    }
}
