using Marvel.View;

namespace Marvel.Godot;

/// <summary>Session-local copy numbers describe visible sources without printing engine handles.</summary>
internal static class SourceCopyIdentity
{
    private static readonly Dictionary<int, Dictionary<string, List<int>>> seats = [];

    internal static IReadOnlyDictionary<int, string> Describe(int seat, IReadOnlyList<BoardCardPresentation> cards)
    {
        if (!seats.TryGetValue(seat, out var titles)) seats[seat] = titles = new(StringComparer.Ordinal);
        foreach (BoardCardPresentation card in cards.OrderBy(card => card.TargetId))
        {
            if (card.TargetId is not { } id) continue;
            if (!titles.TryGetValue(card.Title, out var copies)) titles[card.Title] = copies = [];
            if (!copies.Contains(id)) copies.Add(id);
        }
        return cards.Where(card => card.TargetId is not null).ToDictionary(card => card.TargetId!.Value, card =>
        {
            List<int> copies = titles[card.Title];
            return copies.Count > 1 ? $"copy {copies.IndexOf(card.TargetId!.Value) + 1}" : "";
        });
    }

    internal static void Clear() => seats.Clear();
}
