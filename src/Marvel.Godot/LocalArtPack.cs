using System.Buffers.Binary;
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
    private const long MaximumPackBytes = 64 * 1024 * 1024;
    private const long MaximumManifestBytes = 1024 * 1024;
    private const int MaximumEntries = 2048;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    private const int MaximumDimension = 4096;
    private const long MaximumPixels = MaximumDimension * MaximumDimension;
    private static ReadOnlySpan<byte> PngSignature =>
        [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
    private readonly Dictionary<string, byte[]> files;

    private ArtPackCatalog(Dictionary<string, byte[]> files) => this.files = files;

    /// <summary>Reads a local manifest. Missing or malformed packs are empty.</summary>
    public static ArtPackCatalog Load(string root)
    {
        if (string.IsNullOrWhiteSpace(root) || IsNetworkOrDevicePath(root))
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

            var accepted = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            long acceptedBytes = 0;
            foreach ((string faceId, ArtPackEntry? entry) in manifest.Entries.Take(MaximumEntries))
            {
                long remaining = MaximumPackBytes - acceptedBytes;
                byte[]? asset = ReadAsset(fullRoot, entry, remaining);
                if (asset is not null)
                {
                    accepted.TryAdd(faceId, asset);
                    acceptedBytes += asset.Length;
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

    private static bool IsNetworkOrDevicePath(string path) =>
        path.Contains("://", StringComparison.Ordinal)
        || OperatingSystem.IsWindows()
            && (path.StartsWith("\\\\", StringComparison.Ordinal)
                || path.StartsWith("//", StringComparison.Ordinal)
                || path.StartsWith("\\\\?\\", StringComparison.Ordinal)
                || path.StartsWith("\\\\.\\", StringComparison.Ordinal));

    /// <summary>Whether an authorized, bounded PNG exists for the exact stable face id.</summary>
    public bool Contains(string faceId) =>
        !string.IsNullOrWhiteSpace(faceId) && files.ContainsKey(faceId);

    internal byte[]? Find(string faceId) =>
        !string.IsNullOrWhiteSpace(faceId) && files.TryGetValue(faceId, out byte[]? asset)
            ? asset
            : null;

    private static byte[]? ReadAsset(
        string fullRoot,
        ArtPackEntry? entry,
        long remainingBytes)
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
                || !string.Equals(Path.GetExtension(candidate), ".png", StringComparison.OrdinalIgnoreCase)
                || !File.Exists(candidate)
                || HasLinkInPath(fullRoot, relative))
            {
                return null;
            }

            using FileStream stream = File.Open(
                candidate, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read);
            if (stream.Length is <= 24 or > MaximumFileBytes
                || stream.Length > remainingBytes)
            {
                return null;
            }

            var bytes = new byte[checked((int)stream.Length)];
            stream.ReadExactly(bytes);
            return HasBoundedPngHeader(bytes) ? bytes : null;
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or NotSupportedException)
        {
            return null;
        }
    }

    private static bool HasBoundedPngHeader(byte[] bytes)
    {
        ReadOnlySpan<byte> value = bytes;
        if (!value[..8].SequenceEqual(PngSignature)
            || !value[12..16].SequenceEqual("IHDR"u8))
        {
            return false;
        }

        uint width = BinaryPrimitives.ReadUInt32BigEndian(value[16..20]);
        uint height = BinaryPrimitives.ReadUInt32BigEndian(value[20..24]);
        return width is > 0 and <= MaximumDimension
            && height is > 0 and <= MaximumDimension
            && (long)width * height <= MaximumPixels;
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
