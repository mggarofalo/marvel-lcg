using System.Collections;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Ordered positive placements retained by one enemy attack.</summary>
public sealed class ReceivedAttackDamagePlacements : IReadOnlyList<ReceivedAttackDamage>
{
    private readonly List<ReceivedAttackDamage> placements = [];

    /// <inheritdoc/>
    public int Count => placements.Count;
    /// <inheritdoc/>
    public ReceivedAttackDamage this[int index] => placements[index];
    /// <inheritdoc/>
    public IEnumerator<ReceivedAttackDamage> GetEnumerator() => placements.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void Record(ReceivedAttackDamage placement)
    {
        if (placement.Amount <= 0) throw new ArgumentOutOfRangeException(nameof(placement));
        placements.Add(placement);
    }
}
