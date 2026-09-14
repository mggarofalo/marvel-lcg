using Marvel.View;

namespace Marvel.Godot;

/// <summary>Describes a hand shelf without treating concealed cards as an empty hand.</summary>
internal sealed record TabletopHandShelfSummary(string Heading, string EmptyMessage)
{
    internal static TabletopHandShelfSummary From(
        IReadOnlyList<BoardCardPresentation> visible,
        int concealed)
    {
        ArgumentNullException.ThrowIfNull(visible);
        int count = visible.Sum(card => card.Count);
        string heading = concealed > 0
            ? $"HAND  ·  {count} VISIBLE  ·  {concealed} CONCEALED"
            : $"HAND  ·  {count}";
        string empty = concealed > 0
            ? $"{concealed} concealed card{(concealed == 1 ? string.Empty : "s")} in hand."
            : "Hand is empty.";
        return new TabletopHandShelfSummary(heading, empty);
    }
}
