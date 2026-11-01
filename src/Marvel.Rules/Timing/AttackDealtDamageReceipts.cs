using System.Collections;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>The damage one attack dealt, totalled per recipient.</summary>
/// <remarks>
/// Each entry is the recipient as attacked, with <see cref="Attacked.DamageDealt"/> the total
/// the attack dealt it. One attack has one attacker, the occurrence's actor. Damage dealt is
/// independent of prevention (rr:damage.3.2), and overkill damage is part of the same attack
/// (rr:overkill.2), so a spill adds to its recipient's total.
/// </remarks>
public sealed class AttackDealtDamageReceipts : IReadOnlyList<Attacked>
{
    private readonly List<Attacked> receipts = [];

    /// <inheritdoc/>
    public int Count => receipts.Count;
    /// <inheritdoc/>
    public Attacked this[int index] => receipts[index];
    /// <inheritdoc/>
    public IEnumerator<Attacked> GetEnumerator() => receipts.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void Record(Attacked recipient, long dealt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dealt);
        int index = receipts.FindIndex(receipt => receipt.Card == recipient.Card
            && receipt.Incarnation == recipient.Incarnation);
        // Equivalent mutants: always appending, overwriting the total, or matching the card
        // without its incarnation. No admitted attack places damage on one recipient twice.
        if (index < 0)
            receipts.Add(recipient with { DamageDealt = dealt });
        else
            receipts[index] = receipts[index] with { DamageDealt = receipts[index].DamageDealt + dealt };
    }

    internal void CopyTo(AttackDealtDamageReceipts destination)
    {
        foreach (var receipt in receipts)
            destination.Record(receipt, receipt.DamageDealt);
    }
}
