using Godot;

namespace Marvel.Godot;

/// <summary>Defines the scale-aware interactive button families in the desktop theme.</summary>
internal static class ClientThemeButtons
{
    internal static void Define(
        Theme theme,
        TypeMetrics type,
        ControlMetrics controls,
        DensityMetrics density)
    {
        DefineFamily(theme, new ButtonStyle("Button", ClientTheme.Raised,
            ClientTheme.Alpha(ClientTheme.Outline, 0.58f)), controls, density);
        DefineFamily(theme, new ButtonStyle(GodotThemeVariations.MultiSelectButton,
            ClientTheme.Input, ClientTheme.Alpha(ClientTheme.Outline, 0.62f), Basis: "MenuButton"), controls, density);
        DefineFamily(theme, new ButtonStyle(GodotThemeVariations.ChoiceButton,
            ClientTheme.Raised, ClientTheme.Alpha(ClientTheme.Outline, 0.58f)), controls, density);
        DefineFamily(theme, new ButtonStyle(GodotThemeVariations.LegalTargetButton,
            ClientTheme.Raised.Lightened(0.04f), ClientTheme.Hero, LeftBorder: 4), controls, density);
        DefineFamily(theme, new ButtonStyle(GodotThemeVariations.SelectedTargetButton,
            ClientTheme.Raised.Lightened(0.12f), ClientTheme.Amber, LeftBorder: 7), controls, density);
        DefineFamily(theme, new ButtonStyle(GodotThemeVariations.UnavailableButton,
            ClientTheme.Surface.Darkened(0.08f), ClientTheme.Alpha(ClientTheme.Outline, 0.42f)), controls, density);

        string selected = GodotThemeVariations.SelectedTargetButton;
        foreach (string colorName in new[]
                 {
                     "font_color", "font_hover_color", "font_pressed_color", "font_focus_color",
                 })
        {
            theme.SetColor(colorName, selected, ClientTheme.OnAccent);
        }
        ClientTheme.SetStylebox(theme, "normal", selected, ClientTheme.Flat(
            ClientTheme.Amber, ClientTheme.Amber.Darkened(0.22f), new StyleFrame(2, controls.CornerRadius,
                density.ButtonHorizontal, density.ButtonVertical, LeftBorder: 7)));
        ClientTheme.SetStylebox(theme, "hover", selected, ClientTheme.Flat(
            ClientTheme.Amber.Lightened(0.08f), ClientTheme.OnAccent, new StyleFrame(2, controls.CornerRadius,
                density.ButtonHorizontal, density.ButtonVertical - 1,
                ContentBottom: density.ButtonVertical + 1, LeftBorder: 8)));
        ClientTheme.SetStylebox(theme, "pressed", selected, ClientTheme.Flat(
            ClientTheme.Amber, ClientTheme.OnAccent, new StyleFrame(2, controls.CornerRadius,
                density.ButtonHorizontal, density.ButtonVertical + 2,
                ContentBottom: density.ButtonVertical - 2, LeftBorder: 8)));
        ClientTheme.SetStylebox(theme, "hover_pressed", selected, ClientTheme.Flat(
            ClientTheme.Amber.Lightened(0.08f), ClientTheme.OnAccent, new StyleFrame(3, controls.CornerRadius,
                density.ButtonHorizontal, density.ButtonVertical + 2,
                ContentBottom: density.ButtonVertical - 2, LeftBorder: 8)));
        ClientTheme.SetStylebox(theme, "disabled", GodotThemeVariations.UnavailableButton, ClientTheme.Flat(
            ClientTheme.Surface.Darkened(0.08f), ClientTheme.Alpha(ClientTheme.Outline, 0.42f),
            new StyleFrame(1, controls.CornerRadius,
                density.ButtonHorizontal, density.ButtonVertical, LeftBorder: 2)));

        string primary = GodotThemeVariations.PrimaryButton;
        ClientTheme.Variation(theme, primary, "Button");
        theme.SetColor("font_color", primary, ClientTheme.OnAccent);
        theme.SetColor("font_hover_color", primary, ClientTheme.OnAccent);
        theme.SetColor("font_pressed_color", primary, ClientTheme.OnAccent);
        theme.SetColor("font_disabled_color", primary, ClientTheme.Muted);
        theme.SetFontSize("font_size", primary, type.Body);
        ClientTheme.SetStylebox(theme, "normal", primary, ClientTheme.Flat(
            ClientTheme.Encounter.Darkened(0.14f), ClientTheme.Encounter, new StyleFrame(1, controls.CornerRadius,
                density.PrimaryButtonHorizontal, density.PrimaryButtonVertical, BottomBorder: 4)));
        ClientTheme.SetStylebox(theme, "hover", primary, ClientTheme.Flat(
            ClientTheme.Encounter, ClientTheme.Amber, new StyleFrame(2, controls.CornerRadius,
                density.PrimaryButtonHorizontal, density.PrimaryButtonVertical - 1,
                ContentBottom: density.PrimaryButtonVertical + 1, BottomBorder: 5)));
        ClientTheme.SetStylebox(theme, "pressed", primary, ClientTheme.Flat(
            ClientTheme.Encounter.Darkened(0.28f), ClientTheme.Amber, new StyleFrame(1, controls.CornerRadius,
                density.PrimaryButtonHorizontal, density.PrimaryButtonVertical + 2,
                ContentBottom: density.PrimaryButtonVertical - 2, LeftBorder: 5)));
        ClientTheme.SetStylebox(theme, "hover_pressed", primary, ClientTheme.Flat(
            ClientTheme.Encounter.Darkened(0.18f), ClientTheme.Amber, new StyleFrame(2, controls.CornerRadius,
                density.PrimaryButtonHorizontal, density.PrimaryButtonVertical + 2,
                ContentBottom: density.PrimaryButtonVertical - 2, LeftBorder: 5)));
        ClientTheme.SetStylebox(theme, "focus", primary, ClientTheme.FocusBox(controls));
        ClientTheme.SetStylebox(theme, "disabled", primary, ClientTheme.Flat(
            ClientTheme.Surface.Darkened(0.08f), ClientTheme.Alpha(ClientTheme.Outline, 0.42f),
            new StyleFrame(1, controls.CornerRadius,
                density.PrimaryButtonHorizontal, density.PrimaryButtonVertical)));
    }

    private static void DefineFamily(
        Theme theme,
        ButtonStyle style,
        ControlMetrics controls,
        DensityMetrics density)
    {
        if (style.Variation != style.Basis)
        {
            ClientTheme.Variation(theme, style.Variation, style.Basis);
        }

        theme.SetColor("font_color", style.Variation, ClientTheme.Ink);
        theme.SetColor("font_hover_color", style.Variation, ClientTheme.Ink);
        theme.SetColor("font_pressed_color", style.Variation, ClientTheme.Ink);
        theme.SetColor("font_focus_color", style.Variation, ClientTheme.Ink);
        theme.SetColor("font_disabled_color", style.Variation, ClientTheme.Muted);
        ClientTheme.SetStylebox(theme, "normal", style.Variation, ClientTheme.Flat(
            style.Background, style.Border, new StyleFrame(1, controls.CornerRadius,
                density.ButtonHorizontal, density.ButtonVertical,
                LeftBorder: style.LeftBorder, BottomBorder: 3)));
        ClientTheme.SetStylebox(theme, "hover", style.Variation, ClientTheme.Flat(
            style.Background.Lightened(0.08f), ClientTheme.Amber, new StyleFrame(2, controls.CornerRadius,
                density.ButtonHorizontal, density.ButtonVertical - 1,
                ContentBottom: density.ButtonVertical + 1,
                LeftBorder: Math.Max(2, style.LeftBorder), BottomBorder: 4)));
        ClientTheme.SetStylebox(theme, "pressed", style.Variation, ClientTheme.Flat(
            style.Background.Darkened(0.08f), ClientTheme.Amber, new StyleFrame(1, controls.CornerRadius,
                density.ButtonHorizontal, density.ButtonVertical + 2,
                ContentBottom: density.ButtonVertical - 2,
                LeftBorder: Math.Max(6, style.LeftBorder))));
        ClientTheme.SetStylebox(theme, "hover_pressed", style.Variation, ClientTheme.Flat(
            style.Background, ClientTheme.Amber, new StyleFrame(2, controls.CornerRadius,
                density.ButtonHorizontal, density.ButtonVertical + 2,
                ContentBottom: density.ButtonVertical - 2,
                LeftBorder: Math.Max(6, style.LeftBorder))));
        ClientTheme.SetStylebox(theme, "focus", style.Variation, ClientTheme.FocusBox(controls));
        ClientTheme.SetStylebox(theme, "disabled", style.Variation, ClientTheme.Flat(
            ClientTheme.Surface.Darkened(0.08f), ClientTheme.Alpha(ClientTheme.Outline, 0.42f),
            new StyleFrame(1, 7, density.ButtonHorizontal, density.ButtonVertical)));
    }
}
