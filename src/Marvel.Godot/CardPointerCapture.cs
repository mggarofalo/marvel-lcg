using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Records the original card and release state for one board-owned left-pointer gesture.</summary>
internal sealed class CardPointerCapture
{
    private bool released;

    internal CardPointerCapture(BoardCardPresentation card, bool isHandCard, Vector2 start)
    {
        Card = card;
        IsHandCard = isHandCard;
        Start = start;
    }

    internal BoardCardPresentation Card { get; }
    internal bool IsHandCard { get; }
    internal Vector2 Start { get; }

    internal bool TryReleaseAt(Vector2 finish, out bool isDrag)
    {
        isDrag = false;
        if (released)
        {
            return false;
        }

        released = true;
        isDrag = CardPointerGestureRouter.IsDrag(Start, finish);
        return true;
    }
}
