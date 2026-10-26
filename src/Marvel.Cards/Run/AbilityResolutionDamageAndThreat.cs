using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionDamageAndThreat
{
    internal static bool TryRunDamageAndThreat(this AbilityResolutionExecution execution, AbilityEffect instruction, AbilityEffect syntax, AbilityResolutionState cast)
    {
        if (instruction is AbilityEffect.MoveThreat move)
        {
            execution.MoveThreat(move, cast);
            return true;
        }
        var result = AbilityDamageAndThreatExecution.Run(instruction, syntax, execution.DamageAndThreatContext(cast));
        if (!result.Handled)
        {
            return false;
        }
        execution.ApplyDamageAndThreat(result, syntax, cast);
        return true;
    }

    internal static AbilityDamageAndThreatContext DamageAndThreatContext(this AbilityResolutionExecution execution, AbilityResolutionState cast) =>
        new(cast.ExpressionContext(), execution.program, cast.Trigger, cast.Events,
            cast.AbilityActor, cast.PowerActor, cast.Power, cast.HasContinuation,
            cast.ImminentThreat, cast.ResolutionAbility, cast.Incoming, execution.threatAbilities)
        {
            PowerOccurrence = cast.PowerExecution?.Occurrence,
            DamageActivationId = cast.DamageActivationId,
            DeclaredAttackRecipients = cast.PowerExecution?.Continuation.DeclaredRecipients ?? default,
        };

    internal static void ApplyDamageAndThreat(this AbilityResolutionExecution execution,
        AbilityDamageAndThreatResult result, AbilityEffect syntax, AbilityResolutionState cast)
    {
        RetainDamageCompletion(result, cast);
        if (result.Healed is { } healed) cast.Results["healed"] = healed;
        if (result.SchemesCleared is { } cleared) cast.Results["schemesCleared"] = cleared;
        if (result.Remaining is { } remaining) cast.Replace(remaining);
        cast.Attacked.AddRange(result.Attacked);
        if (result.ResolveEffect) cast.ResolveEffect();
        switch (result.Suspension)
        {
            case AbilityDamageAndThreatSuspension.Choice:
                execution.SuspendForChoice(syntax, cast);
                break;
            case AbilityDamageAndThreatSuspension.Procedure:
                execution.SuspendAfterProcedure(syntax, cast,
                    result.Boundary?.Owner, result.Boundary?.Occurrence);
                break;
            case AbilityDamageAndThreatSuspension.ScheduledThreat:
                execution.ScheduleThreat(result.Threat!, syntax, cast);
                break;
        }
    }
    private static void RetainDamageCompletion(AbilityDamageAndThreatResult result, AbilityResolutionState cast)
    {
        if (result.Damage is { IsComplete: true } receipt) cast.DamageDefeats = receipt.Defeats;
        cast.CompletedOutcome = result.Damage is { IsComplete: true } damage ? damage.Complete() : result.Outcome;
        cast.PendingDamageCompletion = result.Damage is { IsComplete: false } ? result.Damage : null;
    }

    private static void ScheduleThreat(this AbilityResolutionExecution execution,
        ThreatBatch batch, AbilityEffect effect, AbilityResolutionState cast)
    {
        var completion = new EffectCompletion(batch.Amount, batch.Schemes.Count);
        cast.PendingCompletion = completion;
        var capture = AbilityContinuationScheduling.ForEffectProcedure(
            execution.Capture(cast, execution.AbilityOrdinal(effect, cast)));
        var continuation = AbilityContinuations.Step(capture,
            AbilityContinuationStage.Resume, cast.World.Agenda.Current?.Round ?? 0, plan: true);
        Threat.Schedule(cast.World, batch, completion, continuation);
        cast.Suspend();
    }

}
