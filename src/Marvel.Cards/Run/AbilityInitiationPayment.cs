namespace Marvel.Cards.Run;

/// <summary>Retains accepted event and arrow costs across their owned procedures.</summary>
internal sealed class AbilityInitiationPayment(
    AbilityEventPayment? eventPayment, AbilityCostPayment arrowPayment) : AbilityPaymentPlan
{
    private bool initiationOpened;
    private Marvel.Rules.Play.EventPlayInitiation? initiation;
    internal override bool Cancelled => initiation?.Cancelled == true;

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
        if (cast.Suspended) return false;
        if (eventPayment is not null && !initiationOpened)
        {
            initiationOpened = true;
            initiation = AbilityEventInitiation.Open(execution, cast);
            if (initiation is not null)
            {
                cast.Suspend();
                return false;
            }
        }
        return true;
    }
}
