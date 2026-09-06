using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

internal sealed partial class AbilityResolutionExecution
{
    private bool TryRunImmediateEffect(AbilityEffect effect, AbilityResolutionState cast)
    {
        var result = AbilityImmediateExecution.TryRun(effect, new AbilityImmediateContext(
            AdmissionContext(cast), cast.Trigger, cast.Events, cast.GainedKeywords,
            encounterAbilities));
        if (result.ResolveEffect) cast.ResolveEffect();
        return result.Handled;
    }
}
