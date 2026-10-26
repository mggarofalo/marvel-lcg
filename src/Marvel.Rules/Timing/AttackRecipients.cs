namespace Marvel.Rules.Timing;

/// <summary>Ordered physical recipient facts for a single attack occurrence.</summary>
internal sealed class AttackRecipients
{
    private readonly List<State.Attacked> items = [];

    internal IReadOnlyList<State.Attacked> Items => items;

    internal void Add(State.Attacked recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        if (items.Any(item => item.Card == recipient.Card && item.Incarnation == recipient.Incarnation))
            throw new InvalidOperationException("an attack recipient was recorded twice");
        items.Add(recipient);
    }
}
