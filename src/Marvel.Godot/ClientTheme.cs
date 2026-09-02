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
        DefineText(theme, type);
        DefineLayout(theme, VisualSystem.Spacing(scale));
        DefineSurfaces(theme);
        DefineInputs(theme, controls);
        DefineButtons(theme, type, controls);
        DefineOtherControls(theme);
        return theme;
    }

    /// <summary>Converts one framework-independent visual token for Godot.</summary>
    public static Color ToGodot(VisualColor color) => C(color);

    /// <summary>Reads the optional presentation-only desktop scale.</summary>
    public static InterfaceScale ConfiguredScale() =>
        OS.GetEnvironment("MARVEL_UI_SCALE").Trim().ToLowerInvariant() switch
        {
            "large" => InterfaceScale.Large,
            "extra-large" => InterfaceScale.ExtraLarge,
            _ => InterfaceScale.Standard,
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
    }

    private static void DefineSurfaces(Theme theme)
    {
        Panel(theme, GodotThemeVariations.ShellPanel, Flat(
            Surface, Alpha(Outline, 0.46f), 1, 18, 44, 36, 44, 34));
        Panel(theme, GodotThemeVariations.SurfacePanel, Flat(
            Raised, Alpha(Outline, 0.38f), 1, 12, 24, 22, 24, 22));
        Panel(theme, GodotThemeVariations.StatusPanel, Flat(
            Alpha(Amber, 0.14f), Alpha(Amber, 0.62f),
            1, 8, 14, 9, 14, 9, left: 5));
        Panel(theme, GodotThemeVariations.DangerStatusPanel, Flat(
            Alpha(Encounter, 0.2f), Encounter, 2, 8, 14, 9, 14, 9, left: 7));
        Panel(theme, GodotThemeVariations.BoardArea, Flat(
            Raised, Alpha(Outline, 0.5f), 1, 10, 16, 14, 16, 14));
        Panel(theme, GodotThemeVariations.BoardCard, Flat(
            Raised.Lightened(0.06f), Alpha(Amber, 0.58f),
            1, 6, 11, 9, 11, 9, left: 4));
        Panel(theme, GodotThemeVariations.ConcealedCard, Flat(
            Surface.Darkened(0.2f), Alpha(Outline, 0.55f),
            1, 6, 11, 9, 11, 9));
        Panel(theme, GodotThemeVariations.FocusedCard, Flat(
            Raised.Lightened(0.12f), Amber, 3, 6, 11, 9, 11, 9, left: 7));
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

    private static void DefineInputs(Theme theme, ControlMetrics controls)
    {
        StyleBoxFlat normal = Flat(Input, Alpha(Outline, 0.62f),
            1, controls.CornerRadius, 13, 9, 13, 9);
        StyleBoxFlat hover = Flat(
            Raised, Amber, 2, controls.CornerRadius, 13, 9, 13, 9);
        StyleBoxFlat focus = FocusBox(controls);
        StyleBoxFlat disabled = Flat(
            Surface.Darkened(0.08f), Alpha(Outline, 0.42f),
            1, controls.CornerRadius, 13, 9, 13, 9);

        foreach (string type in new[] { "OptionButton", "LineEdit" })
        {
            theme.SetColor("font_color", type, Ink);
            theme.SetColor("font_hover_color", type, Ink);
            theme.SetColor("font_focus_color", type, Ink);
            theme.SetColor("font_disabled_color", type, Muted);
            theme.SetColor("font_uneditable_color", type, Muted);
            theme.SetColor("caret_color", type, Amber);
            theme.SetStylebox("normal", type, normal.Duplicate() as StyleBoxFlat);
            theme.SetStylebox("hover", type, hover.Duplicate() as StyleBoxFlat);
            theme.SetStylebox("pressed", type, hover.Duplicate() as StyleBoxFlat);
            theme.SetStylebox("hover_pressed", type, hover.Duplicate() as StyleBoxFlat);
            theme.SetStylebox("focus", type, focus.Duplicate() as StyleBoxFlat);
            theme.SetStylebox("disabled", type, disabled.Duplicate() as StyleBoxFlat);
            theme.SetStylebox("read_only", type, disabled.Duplicate() as StyleBoxFlat);
        }
    }

    private static void DefineButtons(
        Theme theme,
        TypeMetrics type,
        ControlMetrics controls)
    {
        ButtonSet(theme, "Button", Raised, Alpha(Outline, 0.58f), controls);
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
        theme.SetStylebox("normal", selected, Flat(
            Amber, Amber.Darkened(0.22f), 2, controls.CornerRadius,
            12, 10, 12, 10, left: 7));
        theme.SetStylebox("hover", selected, Flat(
            Amber.Lightened(0.08f), OnAccent, 2, controls.CornerRadius,
            12, 9, 12, 11, left: 8));
        theme.SetStylebox("pressed", selected, Flat(
            Amber, OnAccent, 2, controls.CornerRadius,
            12, 12, 12, 8, left: 8));
        theme.SetStylebox("hover_pressed", selected, Flat(
            Amber.Lightened(0.08f), OnAccent, 3, controls.CornerRadius,
            12, 12, 12, 8, left: 8));

        string unavailable = GodotThemeVariations.UnavailableButton;
        theme.SetStylebox("disabled", unavailable, Flat(
            Surface.Darkened(0.08f), Alpha(Outline, 0.42f),
            1, controls.CornerRadius, 12, 10, 12, 10, left: 2));

        string primary = GodotThemeVariations.PrimaryButton;
        Variation(theme, primary, "Button");
        theme.SetColor("font_color", primary, OnAccent);
        theme.SetColor("font_hover_color", primary, OnAccent);
        theme.SetColor("font_pressed_color", primary, OnAccent);
        theme.SetColor("font_disabled_color", primary, Muted);
        theme.SetFontSize("font_size", primary, type.Body);
        theme.SetStylebox("normal", primary, Flat(
            Encounter.Darkened(0.14f), Encounter, 1, controls.CornerRadius,
            18, 11, 18, 11, bottom: 4));
        theme.SetStylebox("hover", primary, Flat(
            Encounter, Amber, 2, controls.CornerRadius,
            18, 10, 18, 12, bottom: 5));
        theme.SetStylebox("pressed", primary, Flat(
            Encounter.Darkened(0.28f), Amber, 1, controls.CornerRadius,
            18, 13, 18, 9, left: 5));
        theme.SetStylebox("hover_pressed", primary, Flat(
            Encounter.Darkened(0.18f), Amber, 2, controls.CornerRadius,
            18, 13, 18, 9, left: 5));
        theme.SetStylebox("focus", primary, FocusBox(controls));
        theme.SetStylebox("disabled", primary, Flat(
            Surface.Darkened(0.08f), Alpha(Outline, 0.42f),
            1, controls.CornerRadius, 18, 11, 18, 11));
    }

    private static void DefineOtherControls(Theme theme)
    {
        theme.SetColor("font_color", "PopupMenu", Ink);
        theme.SetColor("font_hover_color", "PopupMenu", Ink);
        theme.SetColor("font_disabled_color", "PopupMenu", Muted);
        theme.SetStylebox("panel", "PopupMenu", Flat(
            Surface, Alpha(Outline, 0.62f), 1, 7, 8, 8, 8, 8));
        theme.SetStylebox("hover", "PopupMenu", Flat(
            Raised, Amber, 2, 5, 8, 6, 8, 6, left: 4));
        theme.SetStylebox("separator", "HSeparator", new StyleBoxLine
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
        theme.SetStylebox("panel", variation, style);
    }

    private static void ButtonSet(
        Theme theme,
        string variation,
        Color background,
        Color border,
        ControlMetrics controls,
        int left = 1)
    {
        if (variation != "Button")
        {
            Variation(theme, variation, "Button");
        }

        theme.SetColor("font_color", variation, Ink);
        theme.SetColor("font_hover_color", variation, Ink);
        theme.SetColor("font_pressed_color", variation, Ink);
        theme.SetColor("font_focus_color", variation, Ink);
        theme.SetColor("font_disabled_color", variation, Muted);
        theme.SetStylebox("normal", variation, Flat(
            background, border, 1, controls.CornerRadius,
            12, 10, 12, 10, left: left, bottom: 3));
        theme.SetStylebox("hover", variation, Flat(
            background.Lightened(0.08f), Amber, 2, controls.CornerRadius,
            12, 9, 12, 11,
            left: Math.Max(2, left), bottom: 4));
        theme.SetStylebox("pressed", variation, Flat(
            background.Darkened(0.08f), Amber, 1, controls.CornerRadius,
            12, 12, 12, 8,
            left: Math.Max(6, left)));
        theme.SetStylebox("hover_pressed", variation, Flat(
            background, Amber, 2, controls.CornerRadius, 12, 12, 12, 8,
            left: Math.Max(6, left)));
        theme.SetStylebox("focus", variation, FocusBox(controls));
        theme.SetStylebox("disabled", variation, Flat(
            Surface.Darkened(0.08f), Alpha(Outline, 0.42f),
            1, 7, 12, 10, 12, 10));
    }

    private static void Variation(Theme theme, string variation, string basis) =>
        theme.SetTypeVariation(variation, basis);

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
        float marginLeft,
        float marginTop,
        float marginRight,
        float marginBottom,
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
        ContentMarginLeft = marginLeft,
        ContentMarginTop = marginTop,
        ContentMarginRight = marginRight,
        ContentMarginBottom = marginBottom,
    };

    private static Color C(VisualColor color) => new(
        color.Red / 255.0f,
        color.Green / 255.0f,
        color.Blue / 255.0f);

    private static Color Alpha(Color color, float alpha) => new(color, alpha);
}
