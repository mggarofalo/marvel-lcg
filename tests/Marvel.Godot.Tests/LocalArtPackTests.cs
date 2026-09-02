using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class LocalArtPackTests
{
    private static readonly string[] RejectedFaceIds =
    [
        "unauthorized", "no-rights", "missing", "unsupported", "escaping",
        "url", "large", "null-entry", "absolute",
    ];
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".webp"];

    [Fact]
    public void CatalogAcceptsOnlyAnExactAuthorizedLocalFaceEntry()
    {
        using var pack = new TemporaryPack();
        pack.Write("hero.png", "not needed by the catalog");
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

        Assert.Equal(pack.Path("hero.png"), catalog.Find("01001a"));
        Assert.Null(catalog.Find("01001b"));
        Assert.Null(catalog.Find("01001A"));
        Assert.Null(catalog.Find(""));
    }

    [Fact]
    public void CatalogRejectsUnauthorizedMissingUnsupportedAndEscapingEntries()
    {
        using var pack = new TemporaryPack();
        pack.Write("unauthorized.png", "x");
        pack.Write("rights.png", "x");
        pack.Write("notes.txt", "x");
        pack.Write("valid.png", "catalog only");
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
                "null-entry": null,
                "absolute": { "file": "ABSOLUTE", "authorized": true, "rights": "mine" },
                "valid": { "file": "valid.png", "authorized": true, "rights": "mine" }
              }
            }
            """.Replace("ABSOLUTE", absolute, StringComparison.Ordinal));

        ArtPackCatalog catalog = ArtPackCatalog.Load(pack.Root);

        Assert.All(RejectedFaceIds, faceId => Assert.Null(catalog.Find(faceId)));
        Assert.Equal(pack.Path("valid.png"), catalog.Find("valid"));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{ \"version\": 2, \"entries\": {} }")]
    public void MissingMalformedOrUnknownManifestFallsBackToAnEmptyCatalog(string manifest)
    {
        using var pack = new TemporaryPack();
        pack.Manifest(manifest);

        Assert.Null(ArtPackCatalog.Load(pack.Root).Find("01001a"));
        Assert.Null(ArtPackCatalog.Load(pack.Path("missing")).Find("01001a"));
    }

    [Fact]
    public void RepositoryContainsNoBundledCardImages()
    {
        string client = RepositoryPaths.Repository("src", "Marvel.Godot");
        string[] images = Directory.EnumerateFiles(client, "*", SearchOption.AllDirectories)
            .Where(path => ImageExtensions
                .Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => !path.Contains(
                Path.DirectorySeparatorChar + ".godot" + Path.DirectorySeparatorChar,
                StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(images);
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

        public void WriteLarge(string name, long length)
        {
            using FileStream stream = File.Create(Path(name));
            stream.SetLength(length);
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
