using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionImmediateEffects
{
    internal static bool TryRunImmediateEffect(this AbilityResolutionExecution execution, AbilityEffect effect, AbilityResolutionState cast)
    {
        var result = AbilityImmediateExecution.TryRun(effect, new AbilityImmediateContext(
            execution.AdmissionContext(cast), cast.Trigger, cast.Events, cast.GainedKeywords,
            execution.encounterAbilities));
        if (result.ResolveEffect) cast.ResolveEffect();
        return result.Handled;
    }
}
