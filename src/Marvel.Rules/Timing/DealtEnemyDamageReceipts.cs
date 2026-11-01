using System.Collections;

namespace Marvel.Rules.Timing;

/// <summary>Ordered damage identities dealt to enemies within one occurrence.</summary>
public sealed class DealtEnemyDamageReceipts : IReadOnlyList<DealtEnemyDamage>
{
    private readonly List<DealtEnemyDamage> receipts = [];

    /// <inheritdoc/>
    public int Count => receipts.Count;
    /// <inheritdoc/>
    public DealtEnemyDamage this[int index] => receipts[index];
    /// <inheritdoc/>
    public IEnumerator<DealtEnemyDamage> GetEnumerator() => receipts.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void Record(DealtEnemyDamage receipt)
    {
        if (receipt.Dealt <= 0) throw new ArgumentOutOfRangeException(nameof(receipt));
        receipts.Add(receipt);
    }
}
