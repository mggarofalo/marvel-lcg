using Godot;

namespace Marvel.Godot;

/// <summary>Places direct controls inside a card without changing the card's requested size.</summary>
internal static class CardInteractionLayout
{
    internal static Rect2 Control(int index, float width)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        return new Rect2(12, 16 + 48 * index, width, 44);
    }
}
