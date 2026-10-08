using Godot;

namespace Marvel.Godot;

/// <summary>Pinned card fonts loaded from the assembly without an import or network dependency.</summary>
internal static class CardTypography
{
    internal static Font Title => Load("BarlowCondensed-Bold");
    internal static Font Body => Load("Barlow-Regular");
    internal static Font Bold => Load("Barlow-Bold");
    private static FontVariation? boldItalic;
    internal static Font BoldItalic => boldItalic ??= new FontVariation
    { BaseFont = Italic, VariationEmbolden = 0.6f };
    internal static Font Italic => Load("Barlow-Italic");

    private static FontFile Load(string name)
    {
        FontFile font = PackagedFonts.Load(name);
        if (font.Fallbacks.Count == 0) font.Fallbacks = [PackagedFonts.Load("DejaVuSans")];
        return font;
    }
}
