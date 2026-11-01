using Godot;

namespace Marvel.Godot;

/// <summary>Building block: progress as a label, a row of pips and a count, explained on hover.</summary>
internal static class Tracker
{
    /// <summary>Which pips are filled, in order: filled pips first. Counts below zero draw nothing.</summary>
    internal static bool[] Pips(int filled, int total)
    {
        int all = Math.Max(0, total);
        int full = Math.Clamp(filled, 0, all);
        return [.. Enumerable.Range(0, all).Select(index => index < full)];
    }

    /// <summary>The tracker's accessible reading, such as "Kang (II) 2 of 3, Dominions 1".</summary>
    internal static string Reading(TrackerState state) => TableWording.TrackerReading(state.Label,
        Math.Clamp(state.Filled, 0, Math.Max(0, state.Total)), state.Total, state.Counter?.Label, state.Count);

    /// <summary>The tracker row.</summary>
    internal static HBoxContainer Create(string name, TrackerState state, InterfaceScale scale)
    {
        SpacingMetrics spacing = VisualSystem.Spacing(scale);
        var row = new HBoxContainer
        {
            Name = name, TooltipText = state.Tooltip, AccessibilityName = Reading(state),
            AccessibilityDescription = state.Tooltip, MouseFilter = Control.MouseFilterEnum.Pass,
        };
        row.AddThemeConstantOverride("separation", spacing.Small);
        row.SetMeta("tracker_reading", Reading(state));
        Label label = TableBlockStyle.Whole("Label", state.Label, scale, VisualSystem.Palette.Text);
        label.Visible = state.Label.Length > 0;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.AddThemeFontSizeOverride("font_size", VisualSystem.Type(scale).Heading);
        row.AddChild(label);
        var pips = new HBoxContainer { Name = "Pips", MouseFilter = Control.MouseFilterEnum.Ignore };
        pips.AddThemeConstantOverride("separation", spacing.ExtraSmall);
        foreach (bool filled in Pips(state.Filled, state.Total)) pips.AddChild(Pip(filled, scale));
        row.AddChild(pips);
        if (state.Counter is { } counter) row.AddChild(Chip.Create("Counter", counter, state.Count, scale));
        return row;
    }

    // A pip is a circle; filled and hollow differ by fill, not only by color.
    private static Panel Pip(bool filled, InterfaceScale scale)
    {
        int size = Math.Max(8, VisualSystem.Type(scale).Caption - 2);
        var pip = new Panel
        {
            Name = filled ? "Filled" : "Hollow", CustomMinimumSize = new Vector2(size, size),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        Color ink = ClientTheme.ToGodot(VisualSystem.Palette.Accent);
        using var style = new StyleBoxFlat
        {
            BgColor = filled ? ink : Colors.Transparent, BorderColor = ink,
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = size, CornerRadiusTopRight = size, CornerRadiusBottomLeft = size, CornerRadiusBottomRight = size,
        };
        pip.AddThemeStyleboxOverride("panel", style);
        return pip;
    }
}
