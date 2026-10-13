using Marvel.View;

namespace Marvel.Godot;

/// <summary>Local card-instance slots, including vacancies; never changes engine source order or replay.</summary>
internal static class SourcePresentationOrder
{
    private static readonly Dictionary<int, List<int?>> seats = [];

    internal static BoardCardPresentation[] Arrange(int seat, IReadOnlyList<BoardCardPresentation> cards) =>
        Slots(seat, cards, 0).OfType<BoardCardPresentation>().ToArray();

    internal static BoardCardPresentation?[] Slots(int seat, IReadOnlyList<BoardCardPresentation> cards, int capacity)
    {
        if (!seats.TryGetValue(seat, out List<int?>? order)) seats[seat] = order = [];
        var present = cards.Where(card => card.TargetId is not null).ToDictionary(card => card.TargetId!.Value);
        for (int index = 0; index < order.Count; index++)
            if (order[index] is { } id && !present.ContainsKey(id)) order[index] = null;
        foreach (BoardCardPresentation card in cards)
            if (card.TargetId is { } id && !order.Contains(id)) Insert(order, id);
        FitCapacity(order, capacity);
        return order.Select(id => id is { } card ? present[card] : null).ToArray();
    }

    private static void FitCapacity(List<int?> order, int capacity)
    {
        int last = order.FindLastIndex(id => id is not null);
        int length = Math.Max(capacity, last + 1);
        if (order.Count > length) order.RemoveRange(length, order.Count - length);
        while (order.Count < length) order.Add(null);
    }

    private static void Insert(List<int?> order, int id)
    {
        int vacancy = order.IndexOf(null);
        if (vacancy >= 0) order[vacancy] = id;
        else order.Add(id);
    }

    internal static void Store(int seat, IEnumerable<int?> ids)
    {
        HashSet<int> seen = [];
        seats[seat] = ids.Select(id => id is { } card && seen.Add(card) ? id : null).ToList();
    }

    internal static void Clear() { seats.Clear(); SourceCopyIdentity.Clear(); }
}
