using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionImmediateEffects
{
    internal static bool TryRunImmediateEffect(this AbilityResolutionExecution execution, AbilityEffect effect, AbilityResolutionState cast)
    {
        var owner = cast.World.Agenda.Current;
        var occurrence = cast.World.Agenda.Occurrence;
        var result = AbilityImmediateExecution.TryRun(effect, new AbilityImmediateContext(
            execution.AdmissionContext(cast), cast.Trigger, cast.Events, cast.GainedKeywords,
            execution.encounterAbilities));
        if (result.ResolveEffect) cast.ResolveEffect();
        if (result.ResolveEffect && effect is AbilityEffect.CardAction
            { Instruction: AbilityCardInstruction.DeclareDefender })
        {
            cast.CompletedOutcome = AbilityStructuralOutcome.Full;
            if (result.ProcedureScheduled)
                execution.SuspendAfterProcedure(effect, cast, owner, occurrence);
        }
        return result.Handled;
    }
}
