using Godot;

namespace Marvel.Godot;

/// <summary>Fits canonical marks and original B1 functional outlines to exact native icon bounds.</summary>
internal static class CardGlyphRendering
{
    private static readonly Dictionary<string, Texture2D> Textures = new(StringComparer.Ordinal);

    internal static TextureRect Create(string glyph, string name, Rect2 bounds, Color color)
    {
        return new TextureRect
        {
            Name = name, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = Texture(glyph), Position = bounds.Position, Size = bounds.Size,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
            MouseFilter = Control.MouseFilterEnum.Ignore, Modulate = color,
        };
    }

    private static Texture2D Texture(string glyph)
    {
        if (Textures.TryGetValue(glyph, out Texture2D? cached)) return cached;
        using Stream source = typeof(CardGlyphRendering).Assembly.GetManifestResourceStream(
            $"Marvel.Godot.Assets.CardIcons.{glyph}.svg")
            ?? throw new InvalidOperationException($"Missing card glyph {glyph}");
        using var reader = new StreamReader(source);
        using var image = new Image();
        if (image.LoadSvgFromString(reader.ReadToEnd()) != Error.Ok)
            throw new InvalidOperationException($"Invalid card glyph {glyph}");
        image.FixAlphaEdges();
        image.GenerateMipmaps();
        Texture2D texture = ImageTexture.CreateFromImage(image);
        Textures.Add(glyph, texture);
        return texture;
    }

    internal static string? Stat(string name) => name.TrimEnd('+') switch
    {
        "ATK" or "THW" or "DEF" or "SCH" or "HS" => name.TrimEnd('+'),
        "REC" or "HP" => "heart",
        "StartingThreat" or "EscalationThreat" => "SCH",
        _ => null,
    };

    internal static string Aspect(string classification, CardFrameFamily family) => classification.ToUpperInvariant() switch
    {
        "AGGRESSION" => "ATK", "JUSTICE" => "THW", "LEADERSHIP" => "person",
        "PROTECTION" => "DEF", "BASIC" => "basic", "HERO" => "hero",
        _ => family is CardFrameFamily.Enemy or CardFrameFamily.Scheme or CardFrameFamily.Environment ? "boost" : "hero",
    };
}
