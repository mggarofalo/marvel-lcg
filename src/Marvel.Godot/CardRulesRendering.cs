using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Shapes canonical rules with packaged fonts before the face allocates their paper field.</summary>
internal static class CardRulesRendering
{
    internal static RichTextLabel Create(BoardCardPresentation card, InterfaceScale scale, float fontSize)
    {
        CardRulesMarkup.ResourceFont();
        var rules = new RichTextLabel
        {
            Name = "RulesText", BbcodeEnabled = true,
            Text = CardRulesMarkup.ToBbCode(card.RulesMarkup, card.RulesText, scale,
                Mathf.RoundToInt(fontSize * 1.22f)),
            ScrollActive = false, FitContent = false, MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rules.AddThemeFontOverride("normal_font", CardTypography.Body);
        rules.AddThemeFontOverride("bold_font", CardTypography.Bold);
        rules.AddThemeFontOverride("italics_font", CardTypography.Italic);
        rules.AddThemeFontOverride("bold_italics_font", CardTypography.BoldItalic);
        rules.AddThemeColorOverride("default_color", CardFaceStyle.Ink);
        foreach (string font in new[] { "normal_font_size", "bold_font_size", "italics_font_size", "bold_italics_font_size" })
            rules.AddThemeFontSizeOverride(font, Math.Max(5, Mathf.RoundToInt(fontSize)));
        return rules;
    }
}
