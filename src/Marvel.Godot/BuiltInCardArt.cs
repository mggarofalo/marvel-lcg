using Godot;

namespace Marvel.Godot;

/// <summary>Offline original cut-paper silhouettes, replaceable by any local art provider.</summary>
internal sealed class BuiltInCardArt : ICardArtProvider
{
    internal static BuiltInCardArt Instance { get; } = new();
    private readonly Dictionary<string, Texture2D> textures = new(StringComparer.Ordinal);

    public Texture2D? Find(string faceId)
    {
        var assembly = typeof(BuiltInCardArt).Assembly;
        string resource = $"Marvel.Godot.Assets.Art.{faceId}.svg";
        if (!assembly.GetManifestResourceNames().Contains(resource, StringComparer.Ordinal))
            resource = "Marvel.Godot.Assets.Art.fallback.svg";
        if (textures.TryGetValue(resource, out Texture2D? cached)) return cached;
        using Stream source = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException("The packaged silhouette is unavailable.");
        using var reader = new StreamReader(source);
        using var image = new Image();
        string svg = reader.ReadToEnd()
            .Replace("#132532", "#" + ClientTheme.ToGodot(CardVisualTokens.Ink).ToHtml(false), StringComparison.Ordinal)
            .Replace("#ffffff", "#" + ClientTheme.ToGodot(CardVisualTokens.Paper).ToHtml(false), StringComparison.Ordinal);
        if (image.LoadSvgFromString(svg) != Error.Ok)
            throw new InvalidOperationException("The packaged silhouette could not be decoded.");
        Texture2D texture = ImageTexture.CreateFromImage(image);
        textures.Add(resource, texture);
        return texture;
    }
}
