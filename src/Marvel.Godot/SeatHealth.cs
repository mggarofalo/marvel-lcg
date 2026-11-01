using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>A seat's hit points as a bar and a count; the bar is drawn only when the maximum is published.</summary>
internal static class SeatHealth
{
    /// <summary>The bar's fill, from 0 to 1, or null when either value is unknown or the maximum is not positive.</summary>
    internal static float? Fraction(long? health, long? maximum) =>
        health is { } current && maximum is { } most && most > 0 ? Math.Clamp((float)current / most, 0, 1) : null;

    /// <summary>The identity's current maximum hit points, as the engine publishes them on its card.</summary>
    internal static long? Maximum(BoardPresentation board, int? identity) => identity is { } id
        ? board.Areas.SelectMany(SpatialTableZones.Current).FirstOrDefault(card => card.TargetId == id)
            ?.EffectiveValues.GetValueOrDefault("HP")?.CurrentValue
        : null;

    /// <summary>The row's height.</summary>
    internal static float Height(InterfaceScale scale) => VisualSystem.Type(scale).Caption * 1.4f;

    /// <summary>The bar and its count.</summary>
    internal static HBoxContainer Create(long? health, long? maximum, InterfaceScale scale)
    {
        var row = new HBoxContainer { Name = "Health", MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(0, Height(scale)) };
        row.AddThemeConstantOverride("separation", VisualSystem.Spacing(scale).ExtraSmall);
        if (Fraction(health, maximum) is { } fill)
        {
            var bar = new ProgressBar
            {
                Name = "Bar", MinValue = 0, MaxValue = 1, Value = fill, ShowPercentage = false,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                CustomMinimumSize = new Vector2(0, Math.Max(6, VisualSystem.Spacing(scale).Small)),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            using var back = new StyleBoxFlat { BgColor = ClientTheme.ToGodot(VisualSystem.Palette.Outline) };
            using var front = new StyleBoxFlat { BgColor = ClientTheme.ToGodot(fill <= 0.34f ? VisualSystem.Palette.Danger : VisualSystem.Palette.Legal) };
            bar.AddThemeStyleboxOverride("background", back);
            bar.AddThemeStyleboxOverride("fill", front);
            row.AddChild(bar);
        }
        Label count = TableBlockStyle.Whole("Count", TableWording.Health(health, maximum), scale, VisualSystem.Palette.Text);
        count.SizeFlagsHorizontal = Fraction(health, maximum) is null ? Control.SizeFlags.ExpandFill : Control.SizeFlags.ShrinkEnd;
        row.AddChild(count);
        return row;
    }
}
