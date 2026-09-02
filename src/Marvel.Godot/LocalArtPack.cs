using System.Text.Json;
using Godot;

namespace Marvel.Godot;

/// <summary>Provides optional illustration textures for already-visible card faces.</summary>
public interface ICardArtProvider
{
    /// <summary>Returns local art for a visible stable face id, or null for fallback.</summary>
    Texture2D? Find(string faceId);
}

/// <summary>A fail-closed catalog of authorized files inside one local art-pack root.</summary>
public sealed class ArtPackCatalog
{
    private const long MaximumFileBytes = 20 * 1024 * 1024;
    private const long MaximumManifestBytes = 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    private static readonly HashSet<string> Extensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp" };
    private readonly Dictionary<string, string> files;

    private ArtPackCatalog(Dictionary<string, string> files) => this.files = files;

    /// <summary>Reads a local manifest. Missing or malformed packs are empty.</summary>
    public static ArtPackCatalog Load(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return new ArtPackCatalog([]);
        }

        try
        {
            string fullRoot = Path.GetFullPath(root);
            string manifestPath = Path.Combine(fullRoot, "manifest.json");
            if (!File.Exists(manifestPath)
                || new FileInfo(manifestPath).Length > MaximumManifestBytes)
            {
                return new ArtPackCatalog([]);
            }

            ArtPackManifest? manifest = JsonSerializer.Deserialize<ArtPackManifest>(
                File.ReadAllText(manifestPath), JsonOptions);
            if (manifest?.Version != 1 || manifest.Entries is null)
            {
                return new ArtPackCatalog([]);
            }

            var accepted = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach ((string faceId, ArtPackEntry? entry) in manifest.Entries)
            {
                string? candidate = Resolve(fullRoot, entry);
                if (candidate is not null)
                {
                    accepted.TryAdd(faceId, candidate);
                }
            }

            return new ArtPackCatalog(accepted);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or JsonException
            or ArgumentException
            or NotSupportedException)
        {
            return new ArtPackCatalog([]);
        }
    }

    /// <summary>Returns an authorized local file for the exact stable face id.</summary>
    public string? Find(string faceId) =>
        !string.IsNullOrWhiteSpace(faceId) && files.TryGetValue(faceId, out string? path)
            ? path
            : null;

    private static string? Resolve(string fullRoot, ArtPackEntry? entry)
    {
        if (entry is null
            || !entry.Authorized
            || string.IsNullOrWhiteSpace(entry.Rights)
            || string.IsNullOrWhiteSpace(entry.File)
            || Path.IsPathRooted(entry.File)
            || entry.File.Contains("://", StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            string candidate = Path.GetFullPath(entry.File, fullRoot);
            string relative = Path.GetRelativePath(fullRoot, candidate);
            if (Path.IsPathRooted(relative)
                || relative == ".."
                || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || !Extensions.Contains(Path.GetExtension(candidate))
                || !File.Exists(candidate)
                || new FileInfo(candidate).Length > MaximumFileBytes
                || HasLinkInPath(fullRoot, relative))
            {
                return null;
            }

            return candidate;
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or NotSupportedException)
        {
            return null;
        }
    }

    private static bool HasLinkInPath(string root, string relative)
    {
        string current = root;
        foreach (string part in relative.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                return true;
            }
        }

        return false;
    }

    private sealed record ArtPackManifest(
        int Version,
        Dictionary<string, ArtPackEntry?>? Entries);

    private sealed record ArtPackEntry(
        string File,
        bool Authorized,
        string Rights);
}

/// <summary>Loads and caches illustrations from the configured local art pack.</summary>
public sealed class LocalArtPack : ICardArtProvider
{
    private const int MaximumDimension = 4096;
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
        string? path = catalog.Find(faceId);
        if (path is null)
        {
            return null;
        }

        if (!LooksLikeImage(path))
        {
            return null;
        }

        var image = new Image();
        if (image.Load(path) != Error.Ok
            || image.GetWidth() is <= 0 or > MaximumDimension
            || image.GetHeight() is <= 0 or > MaximumDimension)
        {
            return null;
        }

        return ImageTexture.CreateFromImage(image);
    }

    private static bool LooksLikeImage(string path)
    {
        Span<byte> header = stackalloc byte[12];
        try
        {
            using FileStream stream = File.OpenRead(path);
            int read = stream.Read(header);
            string extension = Path.GetExtension(path);
            return extension.ToLowerInvariant() switch
            {
                ".png" => read >= 8
                    && header[..8].SequenceEqual(
                        new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
                ".jpg" or ".jpeg" => read >= 3
                    && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
                ".webp" => read >= 12
                    && header[..4].SequenceEqual("RIFF"u8)
                    && header[8..12].SequenceEqual("WEBP"u8),
                _ => false,
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
