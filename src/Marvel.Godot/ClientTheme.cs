using Godot;

namespace Marvel.Godot;

/// <summary>Builds the reusable visual vocabulary for the desktop client.</summary>
public static class ClientTheme
{
    private static readonly Color Canvas = C(VisualSystem.Palette.Canvas);
    internal static readonly Color Surface = C(VisualSystem.Palette.Surface);
    internal static readonly Color Raised = C(VisualSystem.Palette.RaisedSurface);
    internal static readonly Color Input = Surface.Darkened(0.18f);
    internal static readonly Color Ink = C(VisualSystem.Palette.Text);
    internal static readonly Color Muted = C(VisualSystem.Palette.MutedText);
    internal static readonly Color OnAccent = C(VisualSystem.Palette.OnAccent);
    internal static readonly Color Amber = C(VisualSystem.Palette.Accent);
    internal static readonly Color Hero = C(VisualSystem.Palette.Legal);
    internal static readonly Color Encounter = C(VisualSystem.Palette.Danger);
    internal static readonly Color Outline = C(VisualSystem.Palette.Outline);

    /// <summary>Creates one theme shared by authored and procedural controls.</summary>
    public static Theme Create(InterfaceScale scale = InterfaceScale.Standard)
    {
        TypeMetrics type = VisualSystem.Type(scale);
        ControlMetrics controls = VisualSystem.Controls(scale);
        DensityMetrics density = VisualSystem.Density(scale);
        var theme = new Theme { DefaultFontSize = type.Body };
        DefineText(theme, type);
        DefineLayout(theme, VisualSystem.Spacing(scale));
        ClientThemeSurfaces.Define(theme, density);
        DefineInputs(theme, controls, density);
        ClientThemeButtons.Define(theme, type, controls, density);
        DefineOtherControls(theme, density);
        return theme;
    }

    /// <summary>Converts one framework-independent visual token for Godot.</summary>
    public static Color ToGodot(VisualColor color) => C(color);

    /// <summary>Reads the optional presentation-only desktop scale.</summary>
    public static InterfaceScale ConfiguredScale() =>
        OS.GetEnvironment("MARVEL_UI_SCALE").Trim().ToLowerInvariant() switch
        {
            "50" or "50%" => InterfaceScale.Percent50,
            "60" or "60%" => InterfaceScale.Percent60,
            "70" or "70%" => InterfaceScale.Percent70,
            "80" or "80%" or "compact" => InterfaceScale.Percent80,
            "90" or "90%" => InterfaceScale.Percent90,
            "100" or "100%" or "standard" => InterfaceScale.Percent100,
            "110" or "110%" => InterfaceScale.Percent110,
            "120" or "120%" or "large" => InterfaceScale.Percent120,
            "130" or "130%" => InterfaceScale.Percent130,
            "140" or "140%" => InterfaceScale.Percent140,
            "150" or "150%" or "extra-large" => InterfaceScale.Percent150,
            _ => InterfaceScale.Compact,
        };

    private static void DefineText(Theme theme, TypeMetrics type)
    {
        theme.SetColor("font_color", "Label", Ink);
        theme.SetFontSize("font_size", "Label", type.Body);
        Label(theme, GodotThemeVariations.DisplayTitle, Ink, type.DisplayTitle, outline: 8);
        Label(theme, GodotThemeVariations.BriefingTitle, Ink,
            (type.DisplayTitle + type.Heading) / 2);
        Label(theme, GodotThemeVariations.Heading, Ink, type.Heading);
        Label(theme, GodotThemeVariations.Body, Ink, type.Body);
        Label(theme, GodotThemeVariations.BodyMuted, Muted, type.Body);
        Label(theme, GodotThemeVariations.MutedText, Muted, type.Caption);
        Label(theme, GodotThemeVariations.Eyebrow, Amber, type.Eyebrow);
        Label(theme, GodotThemeVariations.Caption, Muted, type.Caption);
        Label(theme, GodotThemeVariations.EncounterText, Ink, type.Body);
        Label(theme, GodotThemeVariations.DangerText, Ink, type.Caption);
        Label(theme, GodotThemeVariations.StatusText, Amber, type.Eyebrow);
        Label(theme, GodotThemeVariations.CardTitle, Ink, type.Heading);
        Label(theme, GodotThemeVariations.CardRules, Ink, type.Body);
        Label(theme, GodotThemeVariations.CardLiveValue, Amber, type.Caption);
        Label(theme, GodotThemeVariations.CardPrintedValue, Muted, type.Caption);
        Label(theme, GodotThemeVariations.CardState, Amber, type.Eyebrow);

        theme.SetColor("default_color", "RichTextLabel", Muted);
        theme.SetFontSize("normal_font_size", "RichTextLabel", type.Caption);
        Variation(theme, GodotThemeVariations.CardRulesRich, "RichTextLabel");
        theme.SetColor("default_color", GodotThemeVariations.CardRulesRich, Ink);
        theme.SetFontSize("normal_font_size", GodotThemeVariations.CardRulesRich, type.Body);
        theme.SetFontSize("bold_font_size", GodotThemeVariations.CardRulesRich, type.Body);
        theme.SetFontSize("italics_font_size", GodotThemeVariations.CardRulesRich, type.Body);
    }

    private static void DefineLayout(Theme theme, SpacingMetrics spacing)
    {
        Variation(theme, GodotThemeVariations.TightStack, "VBoxContainer");
        theme.SetConstant("separation", GodotThemeVariations.TightStack, spacing.Small);
        Variation(theme, GodotThemeVariations.Stack, "VBoxContainer");
        theme.SetConstant("separation", GodotThemeVariations.Stack, spacing.Medium);
        Variation(theme, GodotThemeVariations.WideRow, "HBoxContainer");
        theme.SetConstant("separation", GodotThemeVariations.WideRow, spacing.Large);
        Variation(theme, GodotThemeVariations.CompactRow, "HBoxContainer");
        theme.SetConstant("separation", GodotThemeVariations.CompactRow, spacing.ExtraSmall);
        Variation(theme, GodotThemeVariations.DataGrid, "GridContainer");
        theme.SetConstant("h_separation", GodotThemeVariations.DataGrid, spacing.Large);
        theme.SetConstant("v_separation", GodotThemeVariations.DataGrid, spacing.Small);
        theme.SetConstant("h_separation", "HFlowContainer", spacing.Small);
        theme.SetConstant("v_separation", "HFlowContainer", spacing.ExtraSmall);
    }

    private static void DefineInputs(
        Theme theme,
        ControlMetrics controls,
        DensityMetrics density)
    {
        StyleBoxFlat normal = Flat(Input, Alpha(Outline, 0.62f),
            new StyleFrame(1, controls.CornerRadius,
                density.InputHorizontal, density.InputVertical));
        StyleBoxFlat hover = Flat(
            Raised, Amber, new StyleFrame(2, controls.CornerRadius,
                density.InputHorizontal, density.InputVertical));
        StyleBoxFlat focus = FocusBox(controls);
        StyleBoxFlat disabled = Flat(
            Surface.Darkened(0.08f), Alpha(Outline, 0.42f),
            new StyleFrame(1, controls.CornerRadius,
                density.InputHorizontal, density.InputVertical));

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

    private static void DefineOtherControls(Theme theme, DensityMetrics density)
    {
        theme.SetColor("font_color", "PopupMenu", Ink);
        theme.SetColor("font_hover_color", "PopupMenu", Ink);
        theme.SetColor("font_disabled_color", "PopupMenu", Muted);
        SetStylebox(theme, "panel", "PopupMenu", Flat(
            Surface, Alpha(Outline, 0.62f),
            new StyleFrame(1, 7, density.ArtWellInset, density.ArtWellInset)));
        SetStylebox(theme, "hover", "PopupMenu", Flat(
            Raised, Amber, new StyleFrame(2, 5, density.ArtWellInset, density.ArtWellInset, LeftBorder: 4)));
        SetStylebox(theme, "separator", "HSeparator", new StyleBoxLine
        {
            Color = Alpha(Outline, 0.46f),
            Thickness = 1,
            GrowBegin = 0,
            GrowEnd = 0,
        });
        theme.SetConstant("separation", "HSeparator", density.StatusVertical);
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

    internal static void Panel(Theme theme, string variation, StyleBoxFlat style)
    {
        Variation(theme, variation, "PanelContainer");
        SetStylebox(theme, "panel", variation, style);
    }

    internal static void Variation(Theme theme, string variation, string basis) =>
        theme.SetTypeVariation(variation, basis);

    internal static void SetStylebox(
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

    internal static StyleBoxFlat FocusBox(ControlMetrics controls) => new()
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

    internal static StyleBoxFlat Flat(Color background, Color border, StyleFrame frame) => new()
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = frame.LeftBorder ?? frame.BorderWidth,
            BorderWidthTop = frame.BorderWidth,
            BorderWidthRight = frame.BorderWidth,
            BorderWidthBottom = frame.BottomBorder ?? frame.BorderWidth,
            CornerRadiusTopLeft = frame.CornerRadius,
            CornerRadiusTopRight = frame.CornerRadius,
            CornerRadiusBottomLeft = frame.CornerRadius,
            CornerRadiusBottomRight = frame.CornerRadius,
            ContentMarginLeft = frame.ContentHorizontal,
            ContentMarginTop = frame.ContentVertical,
            ContentMarginRight = frame.ContentHorizontal,
            ContentMarginBottom = frame.ContentBottom ?? frame.ContentVertical,
        };

    private static Color C(VisualColor color) => new(
        color.Red / 255.0f,
        color.Green / 255.0f,
        color.Blue / 255.0f);

    internal static Color Alpha(Color color, float alpha) => new(color, alpha);
}
