using Godot;

namespace Marvel.Godot;

/// <summary>Places scaled direct controls inside a card's visible surface.</summary>
internal static class CardInteractionLayout
{
    internal static float ControlWidth(
        float cardWidth,
        float contentMarginLeft,
        float contentMarginRight,
        InterfaceScale scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cardWidth);
        ArgumentOutOfRangeException.ThrowIfNegative(contentMarginLeft);
        ArgumentOutOfRangeException.ThrowIfNegative(contentMarginRight);
        float inset = VisualSystem.Spacing(scale).Medium;
        return Math.Max(0, cardWidth - contentMarginLeft - contentMarginRight - 2 * inset);
    }

    internal static Rect2 Control(int index, float width, InterfaceScale scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        SpacingMetrics spacing = VisualSystem.Spacing(scale);
        ControlMetrics controls = VisualSystem.Controls(scale);
        return new Rect2(
            spacing.Medium,
            spacing.Large + (controls.MinimumPointerTarget + spacing.ExtraSmall) * index,
            width,
            controls.MinimumPointerTarget);
    }

    internal static float RequiredHeight(int count, InterfaceScale scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return count == 0
            ? 0
            : Control(count - 1, 0, scale).End.Y + VisualSystem.Spacing(scale).Large;
    }

    internal static float SurfaceHeight(float baseHeight, int count, InterfaceScale scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(baseHeight);
        return Math.Max(baseHeight, RequiredHeight(count, scale));
    }
}
