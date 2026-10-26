namespace Marvel.Cards.Run;

/// <summary>Accepted payment work retained by one ability's continuation.</summary>
internal abstract class AbilityPaymentPlan
{
    internal virtual bool Cancelled => false;
    internal virtual bool StopsEffect => false;

    internal abstract bool Commit(AbilityResolutionExecution execution, AbilityResolutionState cast);
}
