namespace Marvel.Cards.Run;

/// <summary>Retains accepted event and arrow costs across their owned procedures.</summary>
internal sealed class AbilityInitiationPayment(
    AbilityEventPayment? eventPayment, AbilityCostPayment arrowPayment) : AbilityPaymentPlan
{
    private bool initiationOpened;
    private bool effectPrevented;
    private Marvel.Rules.Play.EventPlayInitiation? initiation;
    internal override bool StopsEffect => arrowPayment.Unpaid || effectPrevented;
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
        if (StopsEffect) return true;
        if (arrowPayment.HasMinionEntryCost)
        {
            effectPrevented = !EffectCanInitiate(execution, cast);
            if (effectPrevented) return true;
        }
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

    private static bool EffectCanInitiate(AbilityResolutionExecution execution, AbilityResolutionState cast)
    {
        var ability = execution.Pending(cast.Source, cast.ResolutionAbility!.Value);
        // The cost may introduce Patrol before step 6 initiates the thwart.
        var context = execution.AdmissionContext(cast);
        if (!AbilityInitiation.LabelsCanInitiate(ability, context)) return false;
        return Marvel.Rules.Play.LabeledAbilities.WouldBeCancelled(cast.World, cast.World.Facts,
                cast.Player, cast.Source, ability.Labels)
            || AbilityAdmission.TargetsAreValid(ability.Effect, context);
    }

}
