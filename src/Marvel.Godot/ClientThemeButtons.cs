using Godot;

namespace Marvel.Godot;

/// <summary>Maps commitment, choice and utility hierarchy to fixed native surfaces.</summary>
internal static class ClientThemeButtons
{
    internal static void Define(Theme theme, TypeMetrics type, ControlMetrics controls)
    {
        theme.SetFontSize("font_size", "Button", Math.Max(14, type.Body));
        theme.SetFontSize("font_size", "OptionButton", Math.Max(14, type.Body));
        DefineChoice(theme, "Button", controls);
        DefineChoice(theme, GodotThemeVariations.MultiSelectButton, controls, "MenuButton");
        DefineChoice(theme, GodotThemeVariations.ChoiceButton, controls);
        DefineChoice(theme, GodotThemeVariations.LegalTargetButton, controls,
            rail: C(VisualSystem.Palette.Legal));
        DefineChoice(theme, GodotThemeVariations.SelectedTargetButton, controls,
            rail: C(VisualSystem.Palette.Selected), selected: true);
        DefineChoice(theme, GodotThemeVariations.UnavailableButton, controls);
        DefineChoice(theme, GodotThemeVariations.WaitingButton, controls);
        theme.SetColor("font_disabled_color", GodotThemeVariations.WaitingButton,
            C(VisualSystem.Palette.Text));
        DefinePrimary(theme, controls);
        DefineUtility(theme, controls);
    }

    private static void DefineChoice(Theme theme, string variation, ControlMetrics controls,
        string basis = "Button", Color? rail = null, bool selected = false)
    {
        if (variation != basis) theme.SetTypeVariation(variation, basis);
        LabelColors(theme, variation, C(VisualSystem.Palette.Text));
        Color normal = C(selected ? ButtonMaterials.SelectedSurface : ButtonMaterials.Secondary);
        Color edge = rail ?? new Color(C(ButtonMaterials.SecondaryEdge), 0.7f);
        int left = rail is null ? 1 : 3;
        Vector4 margins = new(16, 10, 16, 10);
        Set(theme, variation, "normal", Flat(normal, edge, controls, margins, left));
        Set(theme, variation, "hover", Flat(C(ButtonMaterials.SecondaryHover),
            rail ?? C(ButtonMaterials.HoverEdge), controls, margins, left));
        Set(theme, variation, "pressed", Flat(C(ButtonMaterials.SecondaryPressed),
            edge, controls, margins, left));
        Set(theme, variation, "hover_pressed", Flat(C(ButtonMaterials.SecondaryPressed),
            rail ?? C(ButtonMaterials.HoverEdge), controls, margins, left));
        Set(theme, variation, "disabled", Flat(C(ButtonMaterials.UnavailableSurface),
            C(ButtonMaterials.UnavailableEdge), controls, margins, left));
        Set(theme, variation, "focus", Focus(controls, C(VisualSystem.Palette.Focus)));
    }

    private static void DefinePrimary(Theme theme, ControlMetrics controls)
    {
        string variation = GodotThemeVariations.PrimaryButton;
        theme.SetTypeVariation(variation, "Button");
        LabelColors(theme, variation, C(VisualSystem.Palette.OnAccent));
        Vector4 margins = new(20, 10, 20, 10);
        Color edge = C(ButtonMaterials.PrimaryEdge);
        Set(theme, variation, "normal", Flat(C(ButtonMaterials.Primary), edge, controls, margins));
        Set(theme, variation, "hover", Flat(C(ButtonMaterials.PrimaryHover), edge, controls, margins));
        Set(theme, variation, "pressed", Flat(C(ButtonMaterials.PrimaryPressed), edge, controls, margins));
        Set(theme, variation, "hover_pressed", Flat(C(ButtonMaterials.PrimaryPressed), edge, controls, margins));
        Set(theme, variation, "disabled", Flat(C(ButtonMaterials.UnavailableSurface),
            C(ButtonMaterials.UnavailableEdge), controls, margins));
        using var focus = Focus(controls, C(VisualSystem.Palette.OnAccent));
        // The inset dark ring leaves the outer amber edge visible on the table.
        focus.ExpandMarginLeft = focus.ExpandMarginTop = -3;
        focus.ExpandMarginRight = focus.ExpandMarginBottom = -3;
        Set(theme, variation, "focus", focus);
    }

    private static void DefineUtility(Theme theme, ControlMetrics controls)
    {
        string variation = GodotThemeVariations.TertiaryButton;
        theme.SetTypeVariation(variation, "Button");
        LabelColors(theme, variation, C(VisualSystem.Palette.Text));
        Vector4 margins = new(16, 10, 16, 10);
        Set(theme, variation, "normal", Flat(Colors.Transparent, Colors.Transparent, controls, margins));
        Set(theme, variation, "hover", Flat(C(ButtonMaterials.SecondaryHover), Colors.Transparent, controls, margins));
        Set(theme, variation, "pressed", Flat(C(ButtonMaterials.SecondaryPressed), Colors.Transparent, controls, margins));
        Set(theme, variation, "hover_pressed", Flat(C(ButtonMaterials.SecondaryPressed), Colors.Transparent, controls, margins));
        Set(theme, variation, "disabled", Flat(Colors.Transparent, Colors.Transparent, controls, margins));
        Set(theme, variation, "focus", Focus(controls, C(VisualSystem.Palette.Focus)));
    }

    private static void LabelColors(Theme theme, string variation, Color ink)
    {
        foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color",
            "font_hover_pressed_color", "font_focus_color" })
            theme.SetColor(state, variation, ink);
        theme.SetColor("font_disabled_color", variation, C(VisualSystem.Palette.Unavailable));
        theme.SetFont("font", variation, CardTypography.Bold);
        theme.SetConstant("h_separation", variation, 8);
    }

    private static StyleBoxFlat Flat(Color fill, Color edge, ControlMetrics controls,
        Vector4 margins, int left = 1) => new()
    {
        BgColor = fill, BorderColor = edge,
        BorderWidthLeft = left, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
        CornerRadiusTopLeft = controls.CornerRadius, CornerRadiusTopRight = controls.CornerRadius,
        CornerRadiusBottomLeft = controls.CornerRadius, CornerRadiusBottomRight = controls.CornerRadius,
        ContentMarginLeft = margins.X, ContentMarginTop = margins.Y,
        ContentMarginRight = margins.Z, ContentMarginBottom = margins.W,
    };

    private static StyleBoxFlat Focus(ControlMetrics controls, Color color) => new()
    {
        DrawCenter = false, BorderColor = color,
        BorderWidthLeft = controls.FocusRingWidth, BorderWidthTop = controls.FocusRingWidth,
        BorderWidthRight = controls.FocusRingWidth, BorderWidthBottom = controls.FocusRingWidth,
        CornerRadiusTopLeft = controls.CornerRadius, CornerRadiusTopRight = controls.CornerRadius,
        CornerRadiusBottomLeft = controls.CornerRadius, CornerRadiusBottomRight = controls.CornerRadius,
    };

    private static void Set(Theme theme, string variation, string state, StyleBoxFlat style)
    {
        theme.SetStylebox(state, variation, style);
        style.Dispose();
    }

    private static Color C(VisualColor color) => ClientTheme.ToGodot(color);
}
