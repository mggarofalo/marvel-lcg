namespace Marvel.Cards.Run;

/// <summary>Retains accepted event and arrow costs across their owned procedures.</summary>
internal sealed class AbilityInitiationPayment(
    AbilityEventPayment? eventPayment, AbilityCostPayment arrowPayment) : AbilityPaymentPlan
{
    internal override bool Commit(AbilityResolutionExecution execution, AbilityResolutionState cast)
    {
        if (eventPayment is not null)
        {
            if (!eventPayment.Commit(cast.Occurrence, cast.Events))
            {
                cast.Suspend();
                return false;
            }
            cast.PaidWith(eventPayment.Paid);
        }
        execution.ApplyPayment(
            arrowPayment.Commit(cast.Execution!, execution.cardPlayAbilities, cast.Trigger, cast.Events), cast);
        return !cast.Suspended;
    }
}
