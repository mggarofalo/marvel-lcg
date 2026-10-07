namespace Marvel.Godot;

/// <summary>Canonical symbol assignments from the pinned Champions Icons font stylesheet.</summary>
internal static class CardSymbols
{
    private static readonly Dictionary<string, string> Glyphs =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["physical"] = "P", ["energy"] = "E", ["mental"] = "M", ["wild"] = "W",
            ["star"] = "S", ["boost"] = "B", ["per_hero"] = "G", ["unique"] = "U",
            ["acceleration"] = "A", ["amplify"] = "F", ["crisis"] = "C", ["hazard"] = "H",
        };

    internal static bool TryGet(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? glyph) => Glyphs.TryGetValue(name, out glyph);

    internal static string Markup(string glyph, int size) =>
        $"[font={CardRulesMarkup.ResourceFontPath}][font_size={size}]{glyph}[/font_size][/font]";
}
