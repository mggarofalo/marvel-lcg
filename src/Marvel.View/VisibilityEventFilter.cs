using Marvel.Rules.Events;

namespace Marvel.View;

/// <summary>Retains only event subjects that the response can safely identify.</summary>
internal static class VisibilityEventFilter
{
    internal static List<GameEvent> Filter(
        IReadOnlyList<GameEvent> events,
        HashSet<int> addressable,
        HashSet<int> readable)
    {
        var filtered = new List<GameEvent>(events.Count);
        foreach (GameEvent happened in events)
        {
            GameEvent? safe = Filter(happened, addressable, readable);
            if (safe is not null) filtered.Add(safe);
        }

        return filtered;
    }

    private static GameEvent? Filter(
        GameEvent happened, HashSet<int> addressable, HashSet<int> readable) =>
        happened switch
        {
            CardsCreated created => KeepCreated(created, readable),
            CardsMoved moved => KeepMoved(moved, addressable),
            AreaReordered reordered => reordered.Order.All(addressable.Contains) ? reordered : null,
            CardFormChanged changed => readable.Contains(changed.Card) ? changed : null,
            CardsFlipped flipped => KeepFlipped(flipped, addressable),
            CardAttached attached => KeepAttached(attached, addressable),
            CardDetached detached => KeepDetached(detached, addressable),
            ControlChanged changed => addressable.Contains(changed.Card) ? changed : null,
            FieldSet set => readable.Contains(set.Card) ? set : null,
            PlayAreaJoined joined => joined,
            PlayAreaDetached detached => detached,
            _ => throw new InvalidOperationException(
                $"event kind {happened.GetType().Name} has no visibility decision"),
        };

    private static CardAttached? KeepAttached(CardAttached value, HashSet<int> visible) =>
        visible.Contains(value.Card) && visible.Contains(value.Host) ? value : null;

    private static CardDetached? KeepDetached(CardDetached value, HashSet<int> visible) =>
        visible.Contains(value.Card) && visible.Contains(value.Host) ? value : null;

    private static CardsCreated? KeepCreated(CardsCreated created, HashSet<int> visible)
    {
        var cards = created.Cards.Where(card => visible.Contains(card.Id)).ToList();
        return cards.Count == 0 ? null : created with
        {
            Cards = cards,
            Subjects = KeepSubjects(created.Subjects, cards.Select(card => card.Id)),
        };
    }

    private static CardsMoved? KeepMoved(CardsMoved moved, HashSet<int> visible)
    {
        var cards = moved.Cards.Where(card => visible.Contains(card.Card)).ToList();
        return cards.Count == 0 ? null : moved with
        {
            Cards = cards,
            Subjects = KeepSubjects(moved.Subjects, cards.Select(card => card.Card)),
        };
    }

    private static Dictionary<int, string>? KeepSubjects(
        IReadOnlyDictionary<int, string>? subjects, IEnumerable<int> visible)
    {
        if (subjects is null) return null;
        HashSet<int> ids = [.. visible];
        var kept = subjects.Where(subject => ids.Contains(subject.Key))
            .ToDictionary(subject => subject.Key, subject => subject.Value);
        return kept.Count == 0 ? null : kept;
    }

    private static CardsFlipped? KeepFlipped(CardsFlipped flipped, HashSet<int> visible)
    {
        var cards = flipped.Cards.Where(visible.Contains).ToList();
        return cards.Count == 0 ? null : flipped with { Cards = cards };
    }
}
