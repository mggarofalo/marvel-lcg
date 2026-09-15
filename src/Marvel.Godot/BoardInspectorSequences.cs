using Marvel.View;

namespace Marvel.Godot;

/// <summary>Indexes card sequences that are browsable without rendering every card on the table.</summary>
internal sealed class BoardInspectorSequences
{
    private readonly Dictionary<int, IReadOnlyList<BoardCardPresentation>> byCard = [];

    internal void Register(IReadOnlyList<BoardCardPresentation> cards)
    {
        if (cards.Count < 2)
        {
            return;
        }

        foreach (BoardCardPresentation card in cards)
        {
            if (card.TargetId is { } id)
            {
                byCard[id] = cards;
            }
        }
    }

    internal IReadOnlyList<BoardCardPresentation> For(int? id) =>
        id is { } target && byCard.TryGetValue(target, out var cards) ? cards : [];
}
