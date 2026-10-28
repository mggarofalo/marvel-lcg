namespace Marvel.Rules.Timing;

/// <summary>Ordered physical recipient facts for a single attack occurrence.</summary>
internal sealed class AttackRecipients
{
    private readonly List<State.Attacked> items = [];

    internal IReadOnlyList<State.Attacked> Items => items;

    internal void Add(State.Attacked recipient, bool accumulate)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        int index = items.FindIndex(item => item.Card == recipient.Card && item.Incarnation == recipient.Incarnation);
        if (index < 0)
            items.Add(recipient);
        else if (!accumulate)
            throw new InvalidOperationException("an attack recipient was recorded twice");
        else
        {
            // Engine choice: saturate the aggregate; separate damage placements
            // remain exact even if their cumulative total exceeds the numeric range.
            long previous = items[index].DamageTaken;
            long total = recipient.DamageTaken > long.MaxValue - previous
                ? long.MaxValue : previous + recipient.DamageTaken;
            long excess = recipient.ExcessDamage > long.MaxValue - items[index].ExcessDamage
                ? long.MaxValue : items[index].ExcessDamage + recipient.ExcessDamage;
            items[index] = items[index] with { DamageTaken = total, ExcessDamage = excess };
        }
    }

}
