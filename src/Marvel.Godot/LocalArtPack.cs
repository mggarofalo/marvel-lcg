using System.Buffers.Binary;
using System.Text.Json;
using Godot;

namespace Marvel.Godot;

/// <summary>Loads and caches illustrations from the configured local art pack.</summary>
public sealed class LocalArtPack : ICardArtProvider
{
    private const int MaximumTextureDimension = 1024;
    private readonly ArtPackCatalog catalog;
    private readonly Dictionary<string, Texture2D?> textures = new(StringComparer.Ordinal);

    private LocalArtPack(ArtPackCatalog catalog) => this.catalog = catalog;

    /// <summary>Uses MARVEL_ART_PACK or the app's user-data art-pack directory.</summary>
    public static LocalArtPack OpenConfigured()
    {
        string configured = OS.GetEnvironment("MARVEL_ART_PACK");
        string root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(OS.GetUserDataDir(), "art-pack")
            : configured;
        return Open(root);
    }

    /// <summary>Opens one explicit local root, primarily for packaging and verification.</summary>
    public static LocalArtPack Open(string root) => new(ArtPackCatalog.Load(root));

    /// <inheritdoc />
    public Texture2D? Find(string faceId)
    {
        if (textures.TryGetValue(faceId, out Texture2D? cached))
        {
            return cached;
        }

        Texture2D? loaded = Load(faceId);
        textures.Add(faceId, loaded);
        return loaded;
    }

    private ImageTexture? Load(string faceId)
    {
        byte[]? asset = catalog.Find(faceId);
        if (asset is null)
        {
            return null;
        }

        var image = new Image();
        if (image.LoadPngFromBuffer(asset) != Error.Ok)
        {
            return null;
        }

        if (Math.Max(image.GetWidth(), image.GetHeight()) > MaximumTextureDimension)
        {
            float scale = MaximumTextureDimension
                / (float)Math.Max(image.GetWidth(), image.GetHeight());
            image.Resize(
                Math.Max(1, (int)Math.Round(image.GetWidth() * scale)),
                Math.Max(1, (int)Math.Round(image.GetHeight() * scale)),
                Image.Interpolation.Lanczos);
        }

        return ImageTexture.CreateFromImage(image);
    }
}
