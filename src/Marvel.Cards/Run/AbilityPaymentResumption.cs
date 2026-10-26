namespace Marvel.Cards.Run;

/// <summary>Finishes accepted costs before the suspended ability resumes its effect.</summary>
internal static class AbilityPaymentResumption
{
    internal static bool Complete(AbilityResolutionExecution execution,
        AbilityResolutionState cast, AbilityContinuationState resumed)
    {
        if (resumed.Payment is not { } payment) return true;
        cast.PendingPayment = payment;
        var owner = cast.World.Agenda.Current;
        var occurrence = cast.World.Agenda.Occurrence;
        if (!payment.Commit(execution, cast))
        {
            execution.SuspendAfterCost(cast, resumed.Invocation.Address.Ordinal, owner, occurrence);
            cast.CompleteResolution();
            return false;
        }
        cast.PendingPayment = null;
        if (payment.StopsEffect)
        {
            execution.DiscardEvent(cast.Source, cast);
            cast.CompleteResolution();
            return false;
        }
        if (!payment.Cancelled) return true;
        AbilityEventInitiation.CompleteCancellation(execution, cast);
        return false;
    }
}
