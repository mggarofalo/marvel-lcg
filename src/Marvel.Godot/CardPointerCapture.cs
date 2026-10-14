using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Records the original card and release state for one board-owned left-pointer gesture.</summary>
internal sealed class CardPointerCapture
{
    private bool released;
    private bool recognizedDrag;

    internal CardPointerCapture(BoardCardPresentation card, bool isHandCard, Vector2 start)
    {
        Card = card;
        IsHandCard = isHandCard;
        Start = start;
    }

    internal BoardCardPresentation Card { get; }
    internal bool IsHandCard { get; }
    internal Vector2 Start { get; }

    // Interaction policy: crossing the threshold commits this gesture to a drag,
    // including a trajectory that returns to its original grab point.
    internal bool ObserveAt(Vector2 position) =>
        recognizedDrag |= CardPointerGestureRouter.IsDrag(Start, position);

    internal bool TryReleaseAt(Vector2 finish, out bool isDrag)
    {
        isDrag = false;
        if (released)
        {
            return false;
        }

        released = true;
        isDrag = ObserveAt(finish);
        return true;
    }
}
