namespace Marvel.Rules.Play;

/// <summary>Whether all accepted generators and hand discards have finished paying a cost.</summary>
public sealed class ResourcePaymentReceipt
{
    private readonly AbilityExecution execution;
    internal ResourcePaymentReceipt(AbilityExecution execution) => this.execution = execution;
    /// <summary>True only after every contribution and its owned procedures have committed.</summary>
    public bool IsComplete => execution.IsComplete;
}
