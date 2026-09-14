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
    private static readonly Color OnAccent = C(VisualSystem.Palette.OnAccent);
    private static readonly Color Amber = C(VisualSystem.Palette.Accent);
    private static readonly Color Hero = C(VisualSystem.Palette.Legal);
    private static readonly Color Encounter = C(VisualSystem.Palette.Danger);
    private static readonly Color Outline = C(VisualSystem.Palette.Outline);
    /// <summary>Creates one theme shared by authored and procedural controls.</summary>
    public static Theme Create(InterfaceScale scale = InterfaceScale.Standard)
    {
        TypeMetrics type = VisualSystem.Type(scale);
        ControlMetrics controls = VisualSystem.Controls(scale);
        var theme = new Theme { DefaultFontSize = type.Body };
        ClientThemeTypography.Define(theme, type, Ink, Muted, Amber, Canvas);
        ClientThemeLayout.Define(theme, VisualSystem.Spacing(scale));
        ClientThemeSurfaces.Define(theme, Surface, Raised, Outline, Amber, Encounter, Hero);
        DefineInputs(theme, controls);
        DefineButtons(theme, type, controls);
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
            Raised, Amber, 2, controls.CornerRadius, M(13, 9, 13, 9));
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

    private static void DefineButtons(
        Theme theme,
        TypeMetrics type,
        ControlMetrics controls)
    {
        ButtonSet(theme, "Button", Raised, Alpha(Outline, 0.58f), controls);
        ButtonSet(theme, GodotThemeVariations.MultiSelectButton,
            Input, Alpha(Outline, 0.62f), controls, basis: "MenuButton");
        ButtonSet(theme, GodotThemeVariations.ChoiceButton,
            Raised, Alpha(Outline, 0.58f), controls);
        ButtonSet(theme, GodotThemeVariations.LegalTargetButton,
            Raised.Lightened(0.04f), Hero, controls, left: 4);
        ButtonSet(theme, GodotThemeVariations.SelectedTargetButton,
            Raised.Lightened(0.12f), Amber, controls, left: 7);
        ButtonSet(theme, GodotThemeVariations.UnavailableButton,
            Surface.Darkened(0.08f), Alpha(Outline, 0.42f), controls);

        string selected = GodotThemeVariations.SelectedTargetButton;
        foreach (string colorName in new[]
                 {
                     "font_color", "font_hover_color", "font_pressed_color",
                     "font_focus_color",
                 })
        {
            theme.SetColor(colorName, selected, OnAccent);
        }
        SetStylebox(theme, "normal", selected, Flat(
            Amber, Amber.Darkened(0.22f), 2, controls.CornerRadius,
            M(12, 10, 12, 10), left: 7));
        SetStylebox(theme, "hover", selected, Flat(
            Amber.Lightened(0.08f), OnAccent, 2, controls.CornerRadius,
            M(12, 9, 12, 11), left: 8));
        SetStylebox(theme, "pressed", selected, Flat(
            Amber, OnAccent, 2, controls.CornerRadius,
            M(12, 12, 12, 8), left: 8));
        SetStylebox(theme, "hover_pressed", selected, Flat(
            Amber.Lightened(0.08f), OnAccent, 3, controls.CornerRadius,
            M(12, 12, 12, 8), left: 8));

        string unavailable = GodotThemeVariations.UnavailableButton;
        SetStylebox(theme, "disabled", unavailable, Flat(
            Surface.Darkened(0.08f), Alpha(Outline, 0.42f),
            1, controls.CornerRadius, M(12, 10, 12, 10), left: 2));

        string primary = GodotThemeVariations.PrimaryButton;
        Variation(theme, primary, "Button");
        theme.SetColor("font_color", primary, OnAccent);
        theme.SetColor("font_hover_color", primary, OnAccent);
        theme.SetColor("font_pressed_color", primary, OnAccent);
        theme.SetColor("font_disabled_color", primary, Muted);
        theme.SetFontSize("font_size", primary, type.Body);
        SetStylebox(theme, "normal", primary, Flat(
            Encounter.Darkened(0.14f), Encounter, 1, controls.CornerRadius,
            M(18, 11, 18, 11), bottom: 4));
        SetStylebox(theme, "hover", primary, Flat(
            Encounter, Amber, 2, controls.CornerRadius,
            M(18, 10, 18, 12), bottom: 5));
        SetStylebox(theme, "pressed", primary, Flat(
            Encounter.Darkened(0.28f), Amber, 1, controls.CornerRadius,
            M(18, 13, 18, 9), left: 5));
        SetStylebox(theme, "hover_pressed", primary, Flat(
            Encounter.Darkened(0.18f), Amber, 2, controls.CornerRadius,
            M(18, 13, 18, 9), left: 5));
        SetStylebox(theme, "focus", primary, FocusBox(controls));
        SetStylebox(theme, "disabled", primary, Flat(
            Surface.Darkened(0.08f), Alpha(Outline, 0.42f),
            1, controls.CornerRadius, M(18, 11, 18, 11)));
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

    private static void ButtonSet(
        Theme theme,
        string variation,
        Color background,
        Color border,
        ControlMetrics controls,
        int left = 1,
        string basis = "Button")
    {
        if (variation != basis)
        {
            Variation(theme, variation, basis);
        }

        theme.SetColor("font_color", variation, Ink);
        theme.SetColor("font_hover_color", variation, Ink);
        theme.SetColor("font_pressed_color", variation, Ink);
        theme.SetColor("font_focus_color", variation, Ink);
        theme.SetColor("font_disabled_color", variation, Muted);
        SetStylebox(theme, "normal", variation, Flat(
            background, border, 1, controls.CornerRadius,
            M(12, 10, 12, 10), left: left, bottom: 3));
        SetStylebox(theme, "hover", variation, Flat(
            background.Lightened(0.08f), Amber, 2, controls.CornerRadius,
            M(12, 9, 12, 11),
            left: Math.Max(2, left), bottom: 4));
        SetStylebox(theme, "pressed", variation, Flat(
            background.Darkened(0.08f), Amber, 1, controls.CornerRadius,
            M(12, 12, 12, 8),
            left: Math.Max(6, left)));
        SetStylebox(theme, "hover_pressed", variation, Flat(
            background, Amber, 2, controls.CornerRadius, M(12, 12, 12, 8),
            left: Math.Max(6, left)));
        SetStylebox(theme, "focus", variation, FocusBox(controls));
        SetStylebox(theme, "disabled", variation, Flat(
            Surface.Darkened(0.08f), Alpha(Outline, 0.42f),
            1, 7, M(12, 10, 12, 10)));
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
        ExpandMarginLeft = controls.FocusRingWidth,
        ExpandMarginTop = controls.FocusRingWidth,
        ExpandMarginRight = controls.FocusRingWidth,
        ExpandMarginBottom = controls.FocusRingWidth,
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
