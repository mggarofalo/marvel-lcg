namespace Marvel.Cards.Run;

/// <summary>Accepted payment work retained by one ability's continuation.</summary>
internal abstract class AbilityPaymentPlan
{
    internal abstract bool Commit(AbilityResolutionExecution execution, AbilityResolutionState cast);
}
