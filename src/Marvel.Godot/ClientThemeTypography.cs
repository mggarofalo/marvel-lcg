using Godot;

namespace Marvel.Godot;

/// <summary>Defines the semantic typography portion of the client theme.</summary>
internal static class ClientThemeTypography
{
    internal static void Define(
        Theme theme, TypeMetrics type, Color ink, Color muted, Color amber, Color canvas)
    {
        theme.DefaultFont = CardTypography.Body;
        theme.SetFont("normal_font", "RichTextLabel", CardTypography.Body);
        theme.SetFont("bold_font", "RichTextLabel", CardTypography.Bold);
        theme.SetFont("italics_font", "RichTextLabel", CardTypography.Italic);
        theme.SetFont("bold_italics_font", "RichTextLabel", CardTypography.BoldItalic);
        foreach (string variation in new[] { GodotThemeVariations.CardTitle,
            GodotThemeVariations.DisplayTitle, GodotThemeVariations.Heading })
            theme.SetFont("font", variation, CardTypography.Title);
        theme.SetColor("font_color", "Label", ink);
        theme.SetFontSize("font_size", "Label", type.Body);
        Label(theme, GodotThemeVariations.DisplayTitle, ink, type.DisplayTitle, canvas, 8);
        Label(theme, GodotThemeVariations.BriefingTitle, ink, (type.DisplayTitle + type.Heading) / 2);
        Label(theme, GodotThemeVariations.Heading, ink, type.Heading);
        Label(theme, GodotThemeVariations.Body, ink, type.Body);
        Label(theme, GodotThemeVariations.BodyMuted, muted, type.Body);
        Label(theme, GodotThemeVariations.MutedText, muted, type.Caption);
        Label(theme, GodotThemeVariations.Eyebrow, amber, type.Eyebrow);
        Label(theme, GodotThemeVariations.Caption, muted, type.Caption);
        Label(theme, GodotThemeVariations.EncounterText, ink, type.Body);
        Label(theme, GodotThemeVariations.DangerText, ink, type.Caption);
        Label(theme, GodotThemeVariations.StatusText, amber, type.Eyebrow);
        Label(theme, GodotThemeVariations.CardTitle, ink, type.Heading);
        Label(theme, GodotThemeVariations.CardRules, ink, type.Body);
        Label(theme, GodotThemeVariations.CardLiveValue, amber, type.Caption);
        Label(theme, GodotThemeVariations.CardPrintedValue, muted, type.Caption);
        Label(theme, GodotThemeVariations.CardState, amber, type.Eyebrow);
        theme.SetColor("default_color", "RichTextLabel", muted);
        theme.SetFontSize("normal_font_size", "RichTextLabel", type.Caption);
        theme.SetTypeVariation(GodotThemeVariations.CardRulesRich, "RichTextLabel");
        theme.SetColor("default_color", GodotThemeVariations.CardRulesRich, ink);
        theme.SetFontSize("normal_font_size", GodotThemeVariations.CardRulesRich, type.Body);
        theme.SetFontSize("bold_font_size", GodotThemeVariations.CardRulesRich, type.Body);
        theme.SetFontSize("italics_font_size", GodotThemeVariations.CardRulesRich, type.Body);
        theme.SetFontSize("bold_italics_font_size", GodotThemeVariations.CardRulesRich, type.Body);
    }

    private static void Label(
        Theme theme, string variation, Color color, int size, Color canvas = default, int outline = 0)
    {
        theme.SetTypeVariation(variation, "Label");
        theme.SetColor("font_color", variation, color);
        theme.SetFontSize("font_size", variation, size);
        if (outline > 0)
        {
            theme.SetConstant("outline_size", variation, outline);
            Color outlineColor = canvas.Darkened(0.5f);
            outlineColor.A = 0.55f;
            theme.SetColor("font_outline_color", variation, outlineColor);
        }
    }
}
