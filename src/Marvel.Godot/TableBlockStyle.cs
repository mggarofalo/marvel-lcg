using Godot;

namespace Marvel.Godot;

/// <summary>The shared tokens the table's building blocks draw with, sized for a reading scale.</summary>
internal static class TableBlockStyle
{
    /// <summary>An icon's side in pixels: one caption line.</summary>
    internal static int Icon(InterfaceScale scale) => VisualSystem.Type(scale).Caption + 4;

    /// <summary>A rounded surface with an outline; an emphasized one takes the accent edge.</summary>
    internal static StyleBoxFlat Surface(InterfaceScale scale, VisualColor edge, int border = 1)
    {
        SpacingMetrics spacing = VisualSystem.Spacing(scale);
        int radius = VisualSystem.Controls(scale).CornerRadius;
        return new StyleBoxFlat
        {
            BgColor = ClientTheme.ToGodot(VisualSystem.Palette.RaisedSurface), BorderColor = ClientTheme.ToGodot(edge),
            BorderWidthLeft = border, BorderWidthRight = border, BorderWidthTop = border, BorderWidthBottom = border,
            CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
            ContentMarginLeft = spacing.Small, ContentMarginRight = spacing.Small,
            ContentMarginTop = spacing.ExtraSmall / 2f, ContentMarginBottom = spacing.ExtraSmall / 2f,
        };
    }

    /// <summary>A one-line caption that keeps its full width; its text is also the tooltip.</summary>
    internal static Label Whole(string name, string text, InterfaceScale scale, VisualColor color)
    {
        Label label = Line(name, text, scale, color);
        label.ClipText = false;
        label.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        label.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        return label;
    }

    /// <summary>A caption that wraps at word boundaries and is never cut short.</summary>
    internal static Label Wrapped(string name, string text, InterfaceScale scale, VisualColor color)
    {
        Label label = Line(name, text, scale, color);
        label.ClipText = false;
        label.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return label;
    }

    /// <summary>A one-line caption that ellipsizes rather than wrapping; its full text is the tooltip.</summary>
    internal static Label Line(string name, string text, InterfaceScale scale, VisualColor color)
    {
        var label = new Label
        {
            Name = name, Text = text, TooltipText = text, ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            AutowrapMode = TextServer.AutowrapMode.Off, VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Pass, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        label.AddThemeFontSizeOverride("font_size", VisualSystem.Type(scale).Caption);
        label.AddThemeColorOverride("font_color", ClientTheme.ToGodot(color));
        return label;
    }
}
