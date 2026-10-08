using Marvel.View;

namespace Marvel.Godot;

/// <summary>Resolves sources from one authorized snapshot and indexes independent browsing sequences.</summary>
internal sealed class BoardInspectorSequences(BoardPresentation? board = null)
{
    private readonly Dictionary<int, IReadOnlyList<BoardCardPresentation>> byCard = [];

    internal static BoardCardPresentation? Current(BoardPresentation board, int? id) =>
        id is null ? null : board.Areas.SelectMany(area => area.Cards)
            .FirstOrDefault(card => card.TargetId == id && !card.Concealed);

    internal static BoardCardPresentation Source(BoardPresentation? board, BoardCardPresentation source) =>
        board is not null && Current(board, source.TargetId) is { } current
            ? current : source with { TargetId = null };

    internal void Register(IReadOnlyList<BoardCardPresentation> cards)
    {
        foreach (BoardCardPresentation card in cards)
        {
            if (card.TargetId is { } id)
            {
                byCard[id] = cards;
            }
        }
    }

    internal BoardCardPresentation Source(BoardCardPresentation source) =>
        Source(board, source);

    internal IReadOnlyList<BoardCardPresentation> For(int? id) =>
        id is { } target && byCard.TryGetValue(target, out var cards) ? cards : [];
}
