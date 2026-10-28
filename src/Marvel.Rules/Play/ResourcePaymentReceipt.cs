using System.Collections.Immutable;

namespace Marvel.Rules.Play;

/// <summary>Whether all accepted generators and hand discards have finished paying a cost.</summary>
public sealed class ResourcePaymentReceipt
{
    private readonly AbilityExecution execution;
    private readonly Func<string> paid;
    private readonly Func<string> generated;
    private readonly Func<IReadOnlyList<SpentResourceCard>> spentCards;
    internal ResourcePaymentReceipt(AbilityExecution execution, Func<string> paid, Func<string> generated, Func<IReadOnlyList<SpentResourceCard>> spentCards)
    {
        this.execution = execution;
        this.paid = paid;
        this.generated = generated;
        this.spentCards = spentCards;
    }
    /// <summary>Committed paid types, excluding excess generated icons.</summary>
    public string Paid => IsComplete ? paid()
        : throw new InvalidOperationException("payment is not complete");
    /// <summary>Committed generated types, including excess icons.</summary>
    public string Generated => IsComplete ? generated()
        : throw new InvalidOperationException("payment is not complete");
    /// <summary>Physical hand cards committed to generation, including excess.</summary>
    public IReadOnlyList<SpentResourceCard> SpentCards => IsComplete ? spentCards().ToImmutableArray()
        : throw new InvalidOperationException("payment is not complete");
    /// <summary>True only after every contribution and its owned procedures have committed.</summary>
    public bool IsComplete => execution.IsComplete;
}
