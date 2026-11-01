using Godot;
using System.Xml.Linq;

namespace Marvel.Godot;

/// <summary>Fits canonical marks and original B1 functional outlines to exact native icon bounds.</summary>
internal static class CardGlyphRendering
{
    private static readonly Dictionary<string, Texture2D> Textures = new(StringComparer.Ordinal);

    internal static TextureRect Create(string glyph, string name, Rect2 bounds, Color color)
    {
        Vector2 size = new(Math.Max(1, Mathf.FloorToInt(bounds.Size.X)),
            Math.Max(1, Mathf.FloorToInt(bounds.Size.Y)));
        int pixels = (int)Math.Min(size.X, size.Y);
        return new TextureRect
        {
            Name = name, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = Texture(glyph, pixels), Position = bounds.GetCenter() - size / 2, Size = size,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            MouseFilter = Control.MouseFilterEnum.Ignore, Modulate = color,
        };
    }

    private static Texture2D Texture(string glyph, int pixels) =>
        Rasterize($"Marvel.Godot.Assets.CardIcons.{glyph}.svg", pixels);

    /// <summary>Rasterizes one embedded white-outline SVG at an exact square pixel size, once per size.</summary>
    internal static Texture2D Rasterize(string resource, int pixels)
    {
        string key = $"{resource}:{pixels}";
        if (Textures.TryGetValue(key, out Texture2D? cached)) return cached;
        using Stream source = typeof(CardGlyphRendering).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Missing embedded icon {resource}");
        using var reader = new StreamReader(source);
        XElement svg = XElement.Parse(reader.ReadToEnd());
        svg.SetAttributeValue("width", pixels);
        svg.SetAttributeValue("height", pixels);
        DpiTexture texture = DpiTexture.CreateFromString(svg.ToString(SaveOptions.DisableFormatting));
        texture.FixAlphaBorder = true;
        Textures.Add(key, texture);
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
