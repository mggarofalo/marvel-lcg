using Godot;

namespace Marvel.Godot;

/// <summary>Exercises the shipped typography with the nonverbal symbols used by native controls.</summary>
internal static class UiTypographySample
{
    internal const string Characters = "·×–—…‹›Ⅱ↑→↓↗↳↻−≥─▧▰▱▶▸▾◇◎★⚠✓";

    internal static void Add(VBoxContainer parent)
    {
        var rows = new VBoxContainer { Name = "UiTypographySamples" };
        parent.AddChild(rows);
        foreach ((string name, Font font) in new (string, Font)[]
        {
            ("Body", CardTypography.Body), ("Bold", CardTypography.Bold),
            ("Italic", CardTypography.Italic), ("BoldItalic", CardTypography.BoldItalic),
            ("Title", CardTypography.Title),
        })
        {
            var label = new Label { Name = name, Text = string.Join("  ", Characters.ToCharArray()),
                CustomMinimumSize = new Vector2(0, 36), ClipText = true,
                AutowrapMode = TextServer.AutowrapMode.Off };
            label.AddThemeFontOverride("font", font);
            label.AddThemeFontSizeOverride("font_size", 18);
            rows.AddChild(label);
        }
    }
}
