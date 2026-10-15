using Godot;
namespace Marvel.Godot;
/// <summary>Builds the reusable visual vocabulary for the desktop client.</summary>
public static class ClientTheme
{
    private static readonly Color Canvas = C(VisualSystem.Palette.Canvas);
    private static readonly Color Surface = C(VisualSystem.Palette.Surface);
    private static readonly Color Raised = C(VisualSystem.Palette.RaisedSurface);
    private static readonly Color Input = Surface.Darkened(0.18f);
    private static readonly Color Ink = C(VisualSystem.Palette.Text);
    private static readonly Color Muted = C(VisualSystem.Palette.MutedText);
    private static readonly Color Amber = C(VisualSystem.Palette.Accent);
    private static readonly Color Hero = C(VisualSystem.Palette.Legal);
    private static readonly Color Encounter = C(VisualSystem.Palette.Danger);
    private static readonly Color Outline = C(VisualSystem.Palette.Outline);
    /// <summary>Builds a fresh theme for scoped installation on a native owner.</summary>
    internal static Theme Create(InterfaceScale scale = InterfaceScale.Standard)
    {
        TypeMetrics type = VisualSystem.Type(scale);
        ControlMetrics controls = VisualSystem.Controls(scale);
        var theme = new Theme { DefaultFontSize = type.Body };
        ClientThemeTypography.Define(theme, type, Ink, Muted, Amber, Canvas);
        ClientThemeLayout.Define(theme, VisualSystem.Spacing(scale));
        ClientThemeSurfaces.Define(theme, Surface, Raised, Outline, Amber, Encounter, Hero);
        DefineInputs(theme, controls);
        ClientThemeButtons.Define(theme, type, controls);
        DefineOtherControls(theme);
        return theme;
    }

    /// <summary>Converts one framework-independent visual token for Godot.</summary>
    public static Color ToGodot(VisualColor color) => C(color);

    /// <summary>Reads the optional presentation-only desktop scale.</summary>
    public static InterfaceScale ConfiguredScale() => ClientThemeScale.Configured();

    private static void DefineInputs(Theme theme, ControlMetrics controls)
    {
        StyleBoxFlat normal = Flat(Input, Alpha(Outline, 0.62f),
            1, controls.CornerRadius, M(13, 9, 13, 9));
        StyleBoxFlat hover = Flat(
            C(ButtonMaterials.SecondaryHover), Amber, 1, controls.CornerRadius, M(13, 9, 13, 9));
        StyleBoxFlat focus = FocusBox(controls);
        StyleBoxFlat disabled = Flat(
            Surface.Darkened(0.08f), Alpha(Outline, 0.42f),
            1, controls.CornerRadius, M(13, 9, 13, 9));

        foreach (string type in new[] { "OptionButton", "LineEdit" })
        {
            theme.SetColor("font_color", type, Ink);
            theme.SetColor("font_hover_color", type, Ink);
            theme.SetColor("font_focus_color", type, Ink);
            theme.SetColor("font_disabled_color", type, Muted);
            theme.SetColor("font_uneditable_color", type, Muted);
            theme.SetColor("caret_color", type, Amber);
            SetStylebox(theme, "normal", type, normal.Duplicate() as StyleBoxFlat);
            SetStylebox(theme, "hover", type, hover.Duplicate() as StyleBoxFlat);
            SetStylebox(theme, "pressed", type, hover.Duplicate() as StyleBoxFlat);
            SetStylebox(theme, "hover_pressed", type, hover.Duplicate() as StyleBoxFlat);
            SetStylebox(theme, "focus", type, focus.Duplicate() as StyleBoxFlat);
            SetStylebox(theme, "disabled", type, disabled.Duplicate() as StyleBoxFlat);
            SetStylebox(theme, "read_only", type, disabled.Duplicate() as StyleBoxFlat);
        }

        normal.Dispose();
        hover.Dispose();
        focus.Dispose();
        disabled.Dispose();
    }

    private static void DefineOtherControls(Theme theme)
    {
        theme.SetColor("font_color", "PopupMenu", Ink);
        theme.SetColor("font_hover_color", "PopupMenu", Ink);
        theme.SetColor("font_disabled_color", "PopupMenu", Muted);
        SetStylebox(theme, "panel", "PopupMenu", Flat(
            Surface, Alpha(Outline, 0.62f), 1, 7, M(8, 8, 8, 8)));
        SetStylebox(theme, "hover", "PopupMenu", Flat(
            Raised, Amber, 2, 5, M(8, 6, 8, 6), left: 4));
        SetStylebox(theme, "separator", "HSeparator", new StyleBoxLine
        {
            Color = Alpha(Outline, 0.46f),
            Thickness = 1,
            GrowBegin = 0,
            GrowEnd = 0,
        });
        theme.SetConstant("separation", "HSeparator", 10);
    }

    private static void Label(
        Theme theme,
        string variation,
        Color color,
        int size,
        int outline = 0)
    {
        Variation(theme, variation, "Label");
        theme.SetColor("font_color", variation, color);
        theme.SetFontSize("font_size", variation, size);
        if (outline > 0)
        {
            theme.SetConstant("outline_size", variation, outline);
            theme.SetColor("font_outline_color", variation, Alpha(Canvas.Darkened(0.5f), 0.55f));
        }
    }

    private static void Panel(Theme theme, string variation, StyleBoxFlat style)
    {
        Variation(theme, variation, "PanelContainer");
        SetStylebox(theme, "panel", variation, style);
    }

    private static void Variation(Theme theme, string variation, string basis) =>
        theme.SetTypeVariation(variation, basis);

    private static void SetStylebox(
        Theme theme,
        string name,
        string type,
        StyleBox? style)
    {
        ArgumentNullException.ThrowIfNull(style);
        theme.SetStylebox(name, type, style);
        // Theme retains the native resource. Release this temporary managed
        // wrapper now so live theme replacement cannot defer it to shutdown.
        style.Dispose();
    }

    private static StyleBoxFlat FocusBox(ControlMetrics controls) => new()
    {
        DrawCenter = false,
        BorderColor = Amber,
        BorderWidthLeft = controls.FocusRingWidth,
        BorderWidthTop = controls.FocusRingWidth,
        BorderWidthRight = controls.FocusRingWidth,
        BorderWidthBottom = controls.FocusRingWidth,
        CornerRadiusTopLeft = controls.CornerRadius,
        CornerRadiusTopRight = controls.CornerRadius,
        CornerRadiusBottomLeft = controls.CornerRadius,
        CornerRadiusBottomRight = controls.CornerRadius,
        // Bounded scroll surfaces clip outside a control's rectangle. Keep
        // the focus border inside that rectangle so it remains fully visible.
        ExpandMarginLeft = 0,
        ExpandMarginTop = 0,
        ExpandMarginRight = 0,
        ExpandMarginBottom = 0,
    };

    private static StyleBoxFlat Flat(
        Color background,
        Color border,
        int width,
        int radius,
        Vector4 margins,
        int? left = null,
        int? bottom = null) => new()
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = left ?? width,
            BorderWidthTop = width,
            BorderWidthRight = width,
            BorderWidthBottom = bottom ?? width,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ContentMarginLeft = margins.X,
            ContentMarginTop = margins.Y,
            ContentMarginRight = margins.Z,
            ContentMarginBottom = margins.W,
        };

    private static Vector4 M(float left, float top, float right, float bottom) =>
        new(left, top, right, bottom);

    private static Color C(VisualColor color) => new(
        color.Red / 255.0f,
        color.Green / 255.0f,
        color.Blue / 255.0f);

    private static Color Alpha(Color color, float alpha) => new(color, alpha);
}
