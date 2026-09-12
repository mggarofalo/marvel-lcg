using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionDamageAndThreat
{
    // MARVEL-375: syntax identifies the suspended procedure until continuations
    // use execution.program addresses directly. Only the compiled instruction supplies
    // the operation's arguments.
    internal static bool TryRunDamageAndThreat(this AbilityResolutionExecution execution, AbilityEffect instruction, AbilityEffect syntax, AbilityResolutionState cast)
    {
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
            cast.ImminentThreat, cast.ResolutionAbility, cast.Incoming, execution.threatAbilities);

    internal static void ApplyDamageAndThreat(this AbilityResolutionExecution execution,
        AbilityDamageAndThreatResult result, AbilityEffect syntax, AbilityResolutionState cast)
    {
        if (result.Healed is { } healed) cast.Results["healed"] = healed;
        if (result.Remaining is { } remaining) cast.Replace(remaining);
        cast.Attacked.AddRange(result.Attacked);
        if (result.ResolveEffect) cast.ResolveEffect();
        switch (result.Suspension)
        {
            case AbilityDamageAndThreatSuspension.Choice:
                execution.SuspendForChoice(syntax, cast);
                break;
            case AbilityDamageAndThreatSuspension.Procedure:
                execution.SuspendAfterProcedure(syntax, cast);
                break;
            case AbilityDamageAndThreatSuspension.ScheduledThreat:
                cast.Suspend();
                break;
        }
    }
}
