namespace Marvel.Godot;

/// <summary>Shared Impact Editions card colors and metrics. These are product choices.</summary>
public static class CardVisualTokens
{
    public static VisualColor Ink => VisualColor.FromRgb(0x132532);
    public static VisualColor Paper => VisualColor.FromRgb(0xFFFFFF);
    public static VisualColor Table => VisualColor.FromRgb(0xE8EEF0);
    public static VisualColor Secondary => VisualColor.FromRgb(0x506773);
    public static VisualColor Modified => VisualColor.FromRgb(0xE8C877);
    public const int FullTitleSize = 35;
    public const int CompactTitleSize = 16;
    public const int FullBodySize = 17;
    public const float CompactBodySize = 9.3f;
    public const int FullStatSize = 29;
    public const int CompactStatSize = 16;
    public const int FullTraitSize = 11;
    public const float CompactTraitSize = 7.5f;
    public const int ResourceOutline = 1;
    public const int ResourceSize = 28;
    public const int ResourceSlot = 36;
    public const int ResourceFontSize = 30;
    public const int FrameStroke = 2;
    public const int FrameInset = 4;
    public const int FrameRadius = 7;

    /// <summary>Aspect identifies the frame; it never changes a resource's color.</summary>
    public static VisualColor Aspect(string classification, CardFrameFamily family = CardFrameFamily.Identity) => classification.ToUpperInvariant() switch
    {
        "AGGRESSION" => VisualColor.FromRgb(0xAE344A),
        "JUSTICE" => VisualColor.FromRgb(0xAA7A16),
        "LEADERSHIP" => VisualColor.FromRgb(0x2568A1),
        "PROTECTION" => VisualColor.FromRgb(0x2E785C),
        "BASIC" => VisualColor.FromRgb(0x486C7D),
        "ENCOUNTER" => VisualColor.FromRgb(0xBA3B45),
        _ => VisualColor.FromRgb(family is CardFrameFamily.Enemy or CardFrameFamily.Scheme
            or CardFrameFamily.Environment ? 0xBA3B45u : 0x704CADu),
    };

    /// <summary>Canonical resource glyph identities, independent of card aspect or surface.</summary>
    public static VisualColor Resource(char glyph) => glyph switch
    {
        'P' => VisualColor.FromRgb(0xC13547),
        'E' => VisualColor.FromRgb(0xD8AC1A),
        'M' => VisualColor.FromRgb(0x2675B8),
        'W' => VisualColor.FromRgb(0x278554),
        _ => throw new ArgumentOutOfRangeException(nameof(glyph), glyph, "Unknown resource glyph"),
    };
}
