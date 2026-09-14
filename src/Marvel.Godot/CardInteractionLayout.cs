using Godot;

namespace Marvel.Godot;

/// <summary>Places scaled direct controls in a reserved strip below a card's readable surface.</summary>
internal static class CardInteractionLayout
{
    internal static float ControlWidth(float cardWidth, InterfaceScale scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cardWidth);
        float inset = VisualSystem.Spacing(scale).Medium;
        return Math.Max(0, cardWidth - 2 * inset);
    }

    internal static Rect2 Control(float baseHeight, int index, float width, InterfaceScale scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(baseHeight);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        SpacingMetrics spacing = VisualSystem.Spacing(scale);
        ControlMetrics controls = VisualSystem.Controls(scale);
        const int columns = 1;
        float controlWidth = (width - spacing.ExtraSmall * (columns - 1)) / columns;
        int row = index / columns;
        int column = index % columns;
        return new Rect2(
            spacing.Medium + column * (controlWidth + spacing.ExtraSmall),
            baseHeight + spacing.Large
                + (controls.MinimumPointerTarget + spacing.ExtraSmall) * row,
            controlWidth,
            controls.MinimumPointerTarget);
    }

    internal static float RequiredHeight(float baseHeight, int count, float width, InterfaceScale scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(baseHeight);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        return count == 0
            ? baseHeight
            : Control(baseHeight, count - 1, width, scale).End.Y
                + VisualSystem.Spacing(scale).Large;
    }

    internal static float SurfaceHeight(float baseHeight, int count, float width, InterfaceScale scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(baseHeight);
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        return RequiredHeight(baseHeight, count, width, scale);
    }

}
