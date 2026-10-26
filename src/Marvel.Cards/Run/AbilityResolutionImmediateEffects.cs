using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Timing;

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
        RecordOutcome(effect, result, cast);
        ContinueAfterProcedure(execution, effect, result, cast, owner, occurrence);
        return result.Handled;
    }

    private static void RecordOutcome(AbilityEffect effect, AbilityImmediateResult result,
        AbilityResolutionState cast)
    {
        if (effect is AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.CancelBoostIcons })
        {
            cast.Results["cancelledBoostIcons"] = result.CancelledBoostIcons;
            cast.CompletedOutcome = result.CancelledBoostIcons > 0 ? AbilityStructuralOutcome.Full : AbilityStructuralOutcome.None;
        }
        if (effect is AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.CancelBoostAbility })
            cast.CompletedOutcome = result.ResolveEffect ? AbilityStructuralOutcome.Full : AbilityStructuralOutcome.None;
        if (result.ResolveEffect) cast.ResolveEffect();
    }

    private static void ContinueAfterProcedure(AbilityResolutionExecution execution,
        AbilityEffect effect, AbilityImmediateResult result, AbilityResolutionState cast,
        PhaseStep? owner, Occurrence? occurrence)
    {
        bool declaredDefense = result.ResolveEffect && effect is AbilityEffect.CardAction
            { Instruction: AbilityCardInstruction.DeclareDefender };
        if (declaredDefense) cast.CompletedOutcome = AbilityStructuralOutcome.Full;
        if (result.ProcedureScheduled && (declaredDefense || effect is AbilityEffect.GainSurge))
            execution.SuspendAfterProcedure(effect, cast, owner, occurrence);
    }
}
