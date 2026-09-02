using Xunit;

namespace Marvel.Godot.Tests;

public sealed class LocalArtPackTests
{
    private static readonly string[] RejectedFaceIds =
    [
        "unauthorized", "no-rights", "missing", "unsupported", "escaping",
        "url", "large", "null-entry", "absolute",
        "dimensions",
    ];
    [Fact]
    public void CatalogAcceptsOnlyAnExactAuthorizedLocalFaceEntry()
    {
        using var pack = new TemporaryPack();
        pack.WritePngHeader("hero.png", 4, 4);
        pack.Manifest("""
            {
              "version": 1,
              "entries": {
                "01001a": {
                  "file": "hero.png",
                  "authorized": true,
                  "rights": "User confirms permission for this local copy."
                }
              }
            }
            """);

        ArtPackCatalog catalog = ArtPackCatalog.Load(pack.Root);

        Assert.True(catalog.Contains("01001a"));
        Assert.False(catalog.Contains("01001b"));
        Assert.False(catalog.Contains("01001A"));
        Assert.False(catalog.Contains(""));
    }

    [Fact]
    public void CatalogRejectsUnauthorizedMissingUnsupportedAndEscapingEntries()
    {
        using var pack = new TemporaryPack();
        pack.Write("unauthorized.png", "x");
        pack.Write("rights.png", "x");
        pack.Write("notes.txt", "x");
        pack.WritePngHeader("valid.png", 4, 4);
        pack.WritePngHeader("dimensions.png", 8192, 8192);
        pack.WriteLarge("large.png", 20 * 1024 * 1024 + 1L);
        string absolute = pack.Path("unauthorized.png").Replace("\\", "\\\\", StringComparison.Ordinal);
        pack.Manifest("""
            {
              "version": 1,
              "entries": {
                "unauthorized": { "file": "unauthorized.png", "authorized": false, "rights": "none" },
                "no-rights": { "file": "rights.png", "authorized": true, "rights": "" },
                "missing": { "file": "missing.png", "authorized": true, "rights": "mine" },
                "unsupported": { "file": "notes.txt", "authorized": true, "rights": "mine" },
                "escaping": { "file": "../outside.png", "authorized": true, "rights": "mine" },
                "url": { "file": "https://example.com/card.png", "authorized": true, "rights": "mine" },
                "large": { "file": "large.png", "authorized": true, "rights": "mine" },
                "dimensions": { "file": "dimensions.png", "authorized": true, "rights": "mine" },
                "null-entry": null,
                "absolute": { "file": "ABSOLUTE", "authorized": true, "rights": "mine" },
                "valid": { "file": "valid.png", "authorized": true, "rights": "mine" }
              }
            }
            """.Replace("ABSOLUTE", absolute, StringComparison.Ordinal));

        ArtPackCatalog catalog = ArtPackCatalog.Load(pack.Root);

        Assert.All(RejectedFaceIds, faceId => Assert.False(catalog.Contains(faceId)));
        Assert.True(catalog.Contains("valid"));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{ \"version\": 2, \"entries\": {} }")]
    public void MissingMalformedOrUnknownManifestFallsBackToAnEmptyCatalog(string manifest)
    {
        using var pack = new TemporaryPack();
        pack.Manifest(manifest);

        Assert.False(ArtPackCatalog.Load(pack.Root).Contains("01001a"));
        Assert.False(ArtPackCatalog.Load(pack.Path("missing")).Contains("01001a"));
    }

    private sealed class TemporaryPack : IDisposable
    {
        public TemporaryPack()
        {
            Root = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), $"marvel-art-pack-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public string Path(string name) => System.IO.Path.Combine(Root, name);

        public void Manifest(string json) => File.WriteAllText(Path("manifest.json"), json);

        public void Write(string name, string content) => File.WriteAllText(Path(name), content);

        public void WritePngHeader(string name, uint width, uint height)
        {
            byte[] bytes =
            [
                0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a,
                0x00, 0x00, 0x00, 0x0d, 0x49, 0x48, 0x44, 0x52,
                (byte)(width >> 24), (byte)(width >> 16), (byte)(width >> 8), (byte)width,
                (byte)(height >> 24), (byte)(height >> 16), (byte)(height >> 8), (byte)height,
                0x00,
            ];
            File.WriteAllBytes(Path(name), bytes);
        }

        public void WriteLarge(string name, long length)
        {
            using FileStream stream = File.Create(Path(name));
            stream.SetLength(length);
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
