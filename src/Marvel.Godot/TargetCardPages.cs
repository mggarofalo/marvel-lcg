using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Pages current offered target identities without keeping a second selection draft.</summary>
internal sealed class TargetCardPages
{
    private DecisionComposer? draft;
    private int capacity = 1;
    private int[] offered = [];
    internal int Page { get; private set; }
    internal int Count => Math.Max(1, (offered.Length + capacity - 1) / capacity);
    internal IReadOnlyList<int> Current => offered.Skip(Page * capacity).Take(capacity).ToArray();

    internal void Refresh(DecisionComposer composer, int pageCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageCapacity);
        if (!ReferenceEquals(draft, composer)) Page = 0;
        draft = composer;
        capacity = pageCapacity;
        offered = [.. composer.Selected?.Targets?.Legal.Distinct() ?? []];
        Page = Math.Clamp(Page, 0, Count - 1);
    }

    internal void Move(int offset) => Page = Math.Clamp(Page + offset, 0, Count - 1);

    internal IReadOnlyList<BoardCardPresentation> ReadableCards(BoardPresentation board) =>
        offered.Select(id => board.Areas.SelectMany(area => area.Cards)
            .FirstOrDefault(card => card.TargetId == id && !card.Concealed))
            .OfType<BoardCardPresentation>().ToArray();
}
