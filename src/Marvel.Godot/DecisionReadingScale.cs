using Godot;

namespace Marvel.Godot;

/// <summary>Keeps task text readable while the table's physical cards retain their bounded size.</summary>
internal static class DecisionReadingScale
{
    internal static void Apply(Control root)
    {
        if (!InteractionControl.IsUsable(root)) return;
        foreach (Control control in root.FindChildren("*", "Control", true, false).OfType<Control>())
        {
            if (CardInspectorFocus.IsInsideCard(control)) continue;
            int floor = control.ThemeTypeVariation == GodotThemeVariations.Heading ? 18
                : control.ThemeTypeVariation == GodotThemeVariations.Caption ? 13 : 15;
            if (control is Label or Button)
                control.AddThemeFontSizeOverride("font_size", Math.Max(floor, control.GetThemeFontSize("font_size")));
            if (control is RichTextLabel)
                foreach (string font in new[] { "normal_font_size", "bold_font_size", "italics_font_size", "bold_italics_font_size" })
                    control.AddThemeFontSizeOverride(font, Math.Max(floor, control.GetThemeFontSize(font)));
        }
    }
}
