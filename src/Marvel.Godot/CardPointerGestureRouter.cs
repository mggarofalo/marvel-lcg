using Godot;

namespace Marvel.Godot;

/// <summary>Classifies a card pointer release before a board operation is considered.</summary>
internal static class CardPointerGestureRouter
{
    internal const float DragThreshold = 10;

    internal static bool IsDrag(Vector2 start, Vector2 finish) =>
        start.DistanceTo(finish) >= DragThreshold;
}
