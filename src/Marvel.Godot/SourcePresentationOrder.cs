using Marvel.View;

namespace Marvel.Godot;

/// <summary>Local card-instance ordering; never changes engine source order or replay.</summary>
internal static class SourcePresentationOrder
{
    private static readonly Dictionary<int, List<int>> seats = [];

    internal static BoardCardPresentation[] Arrange(int seat, IReadOnlyList<BoardCardPresentation> cards)
    {
        if (!seats.TryGetValue(seat, out List<int>? order)) seats[seat] = order = [];
        HashSet<int> present = cards.Where(card => card.TargetId is not null)
            .Select(card => card.TargetId!.Value).ToHashSet();
        order.RemoveAll(id => !present.Contains(id));
        foreach (BoardCardPresentation card in cards)
            if (card.TargetId is { } id && !order.Contains(id)) order.Add(id);
        return [.. cards.OrderBy(card => card.TargetId is { } id ? order.IndexOf(id) : int.MaxValue)];
    }

    internal static void Store(int seat, IEnumerable<int> ids) => seats[seat] = [.. ids.Distinct()];
    internal static void Clear() { seats.Clear(); SourceCopyIdentity.Clear(); }
}

