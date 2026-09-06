using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

internal sealed partial class AbilityResolutionExecution
{
    // MARVEL-375: syntax identifies the suspended procedure until continuations
    // use program addresses directly. Only the compiled instruction supplies
    // the operation's arguments.
    private bool TryRunDamageAndThreat(AbilityEffect instruction, AbilityEffect syntax, AbilityResolutionState cast)
    {
        var result = AbilityDamageAndThreatExecution.Run(instruction, syntax, DamageAndThreatContext(cast));
        if (!result.Handled)
        {
            return false;
        }
        ApplyDamageAndThreat(result, syntax, cast);
        return true;
    }

    private AbilityDamageAndThreatContext DamageAndThreatContext(AbilityResolutionState cast) =>
        new(cast.ExpressionContext(), program, cast.Trigger, cast.Events,
            cast.AbilityActor, cast.PowerActor, cast.Power, cast.HasContinuation,
            cast.ImminentThreat, cast.ResolutionAbility, cast.Incoming);

    private void ApplyDamageAndThreat(
        AbilityDamageAndThreatResult result, AbilityEffect syntax, AbilityResolutionState cast)
    {
        if (result.Healed is { } healed) cast.Results["healed"] = healed;
        if (result.Remaining is { } remaining) cast.Replace(remaining);
        cast.Attacked.AddRange(result.Attacked);
        if (result.ResolveEffect) cast.ResolveEffect();
        switch (result.Suspension)
        {
            case AbilityDamageAndThreatSuspension.Choice:
                SuspendForChoice(syntax, cast);
                break;
            case AbilityDamageAndThreatSuspension.Procedure:
                SuspendAfterProcedure(syntax, cast);
                break;
            case AbilityDamageAndThreatSuspension.ScheduledThreat:
                cast.Suspend();
                break;
        }
    }
}
