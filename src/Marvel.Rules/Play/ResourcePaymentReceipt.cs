namespace Marvel.Rules.Play;

/// <summary>Whether all accepted generators and hand discards have finished paying a cost.</summary>
public sealed class ResourcePaymentReceipt
{
    private readonly AbilityExecution execution;
    private readonly Func<string> paid;
    internal ResourcePaymentReceipt(AbilityExecution execution, Func<string> paid)
    {
        this.execution = execution;
        this.paid = paid;
    }
    /// <summary>Committed paid types, excluding excess generated icons.</summary>
    public string Paid => IsComplete ? paid()
        : throw new InvalidOperationException("payment is not complete");
    /// <summary>True only after every contribution and its owned procedures have committed.</summary>
    public bool IsComplete => execution.IsComplete;
}
