namespace Marvel.Godot;

/// <summary>Canonical symbol assignments from the pinned Champions Icons font stylesheet.</summary>
internal static class CardSymbols
{
    private static readonly Dictionary<string, string> Glyphs =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["physical"] = "P", ["energy"] = "E", ["mental"] = "M", ["wild"] = "W",
            ["cost"] = "D", ["star"] = "S", ["boost"] = "B", ["per_hero"] = "G", ["unique"] = "U",
            ["acceleration"] = "A", ["amplify"] = "F", ["crisis"] = "C", ["hazard"] = "H",
        };

    internal static bool TryGet(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? glyph) => Glyphs.TryGetValue(name, out glyph);

    internal static string Markup(string glyph, int size)
    {
        string markup = $"[font={CardRulesMarkup.ResourceFontPath}][font_size={size}]{glyph}[/font_size][/font]";
        if (glyph.Length != 1 || !"PEMW".Contains(glyph, StringComparison.Ordinal)) return markup;
        VisualColor color = CardVisualTokens.Resource(glyph[0]);
        VisualColor ink = CardVisualTokens.Ink;
        return $"[outline_size={CardVisualTokens.ResourceOutline}][outline_color=#{ink.Red:X2}{ink.Green:X2}{ink.Blue:X2}]"
            + $"[color=#{color.Red:X2}{color.Green:X2}{color.Blue:X2}]{markup}[/color]"
            + "[/outline_color][/outline_size]";
    }
}
