using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    // MARVEL-375: syntax identifies the suspended procedure until continuations
    // use program addresses directly. Only the compiled instruction supplies
    // the operation's arguments.
    private static bool TryRunDamageAndThreat(AbilityEffect instruction, AbilityEffect syntax, Cast cast)
    {
        var result = AbilityDamageAndThreatExecution.Run(instruction, syntax, DamageAndThreatContext(cast));
        if (!result.Handled)
        {
            return false;
        }
        ApplyDamageAndThreat(result, syntax, cast);
        return true;
    }

    private static AbilityDamageAndThreatContext DamageAndThreatContext(Cast cast) =>
        new(cast.ExpressionContext(), cast.Trigger, cast.Events,
            cast.AbilityActor, cast.PowerActor, cast.Power, cast.HasContinuation,
            cast.ImminentThreat, cast.ResolutionAbility, cast.Incoming);

    private static void ApplyDamageAndThreat(
        AbilityDamageAndThreatResult result, AbilityEffect syntax, Cast cast)
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
