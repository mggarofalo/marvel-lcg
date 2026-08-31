using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Marvel.Rules.Packs.Harvest;
using Marvel.Tests;
using Xunit;
using PackHarvest = Marvel.Rules.Packs.Harvest.Harvest;

namespace Marvel.Rules.Tests.Rules;

public sealed class PackRulesHarvestTests
{
    [Theory]
    [InlineData("mc02_green_goblin_rules_insert.pdf", "mc02", "insert")]
    [InlineData("mc09_hulk_rulesheet.pdf", "mc09", "rulesheet")]
    [InlineData("mc45_age_of_apocalypse_rulebook.pdf", "mc45", "rulebook")]
    [InlineData("mvc01_learn_to_play_eng.pdf", "mvc01", "learn-to-play")]
    public void SourceNamesDeterminePackAndDocumentKind(
        string filename,
        string code,
        string kind)
    {
        var classified = PackHarvest.Classify(filename);
        Assert.NotNull(classified);
        Assert.Equal((code, kind), classified.Value);
    }

    [Fact]
    public void CampaignLogsAndRulesReferencesAreOutsideThePackCorpus()
    {
        Assert.Null(PackHarvest.Classify("mc50_agents_campaign_log.pdf"));
        Assert.Null(PackHarvest.Classify("mc_rulesreference_v18.pdf"));
    }

    [Theory]
    [InlineData(".pdf")]
    [InlineData("rules_insert.pdf")]
    [InlineData("mc_rules_insert.pdf")]
    public void UnsupportedPdfNamesAreRejected(string filename) =>
        Assert.Throws<InvalidDataException>(() => PackHarvest.Classify(filename));

    [Fact]
    public void AGeneratedDestinationCannotEscapeTheOutputRoot()
    {
        string temporary = Path.Combine(Path.GetTempPath(), $"marvel-pack-write-{Guid.NewGuid():N}");
        string root = Path.Combine(temporary, "snapshot");
        string escaped = Path.Combine(temporary, "escaped.md");
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "existing.md"), "preserved");

            Assert.Throws<InvalidDataException>(() => Emit.Write(
                new Dictionary<string, string> { ["../escaped.md"] = "unsafe" },
                root));

            Assert.False(File.Exists(escaped));
            Assert.Equal("preserved", File.ReadAllText(Path.Combine(root, "existing.md")));
        }
        finally
        {
            if (Directory.Exists(temporary))
            {
                Directory.Delete(temporary, recursive: true);
            }
        }
    }

    [Fact]
    public void SourceDiscoveryRejectsAnUnsupportedPdfBeforeReadingIt()
    {
        string temporary = Path.Combine(Path.GetTempPath(), $"marvel-pack-source-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(temporary);
            File.WriteAllBytes(Path.Combine(temporary, ".pdf"), []);

            Assert.Throws<InvalidDataException>(() => PackHarvest.Sources(temporary));
        }
        finally
        {
            if (Directory.Exists(temporary))
            {
                Directory.Delete(temporary, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData("HHEERROO PPAACCKK", "hero-pack")]
    [InlineData("When the Villain Changes Form", "when-the-villain-changes-form")]
    [InlineData("AMPLIFY ICON ([amplify])", "amplify-icon")]
    public void HeadingsHaveStableCitationSlugs(string heading, string slug) =>
        Assert.Equal(slug, PackHarvest.Slug(heading));

    [Theory]
    [InlineData("Ƃfter", "(cid:386)fter")]
    [InlineData("villain’s", "villain’s")]
    [InlineData("", "")]
    public void PdfGlyphsKeepKnownTextAndExposeUnmappedCharacterIds(string extracted, string decoded) =>
        Assert.Equal(decoded, Pages.Decoded(extracted));

    [Fact]
    public void ASectionEmitsNamedRulesAsAnchoredRecords()
    {
        var section = new Section("NEW RULES", 4);
        section.Paragraphs.Add("An opening rule.");
        var rule = new NamedRule("When the Villain Changes Form");
        rule.Paragraphs.Add("The villain keeps each attachment.");
        section.Rules.Add(rule);
        var document = new PackDocument(
            "mc02_rules_insert.pdf",
            "mc02",
            "insert",
            "GREEN GOBLIN",
            [section]);

        IReadOnlyDictionary<string, string> tree = Emit.Build([document]);
        string markdown = tree["mc02/new-rules.md"];
        using var index = JsonDocument.Parse(tree["index.json"]);
        var records = index.RootElement.GetProperty("entries").EnumerateArray().ToList();

        Assert.Equal(2, records.Count);
        Assert.Equal("pack:mc02:new-rules.when-the-villain-changes-form",
            records[1].GetProperty("id").GetString());
        Assert.Contains(
            "<a id=\"when-the-villain-changes-form\"></a>",
            markdown,
            StringComparison.Ordinal);
        Assert.StartsWith("sha256:", records[1].GetProperty("hash").GetString()!, StringComparison.Ordinal);
    }

    [Fact]
    public void TheVendoredIndexIsInternallyConsistent()
    {
        using var index = JsonDocument.Parse(File.ReadAllBytes(
            RepositoryPaths.Dataset("rules-packs", "index.json")));
        var records = index.RootElement.GetProperty("entries").EnumerateArray().ToList();
        var ids = records.Select(record => record.GetProperty("id").GetString()!).ToList();

        Assert.Equal(859, records.Count);
        Assert.Equal(61, index.RootElement.GetProperty("documents").GetInt32());
        Assert.Equal(records.Count, index.RootElement.GetProperty("record_count").GetInt32());
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.All(records, record =>
        {
            string id = record.GetProperty("id").GetString()!;
            Assert.StartsWith("pack:", id, StringComparison.Ordinal);
            Assert.StartsWith("sha256:", record.GetProperty("hash").GetString()!, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(record.GetProperty("fragment").GetString()));

            string[] parts = id.Split(':', 3);
            string section = parts[2].Split('.', 2)[0];
            Assert.True(File.Exists(RepositoryPaths.Dataset("rules-packs", parts[1], section + ".md")));
        });
    }

    [Fact]
    public void TheSourceManifestPinsTheCurrentVendoredSnapshot()
    {
        string root = RepositoryPaths.Dataset("rules-packs");
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(
            Path.Combine(root, "sources.manifest.json")));
        var files = manifest.RootElement.GetProperty("files").EnumerateArray().ToList();
        var actual = IndependentSnapshotHash(root);

        Assert.Equal(61, files.Count);
        Assert.Equal(files.Count, files.Select(file => file.GetProperty("path").GetString())
            .Distinct(StringComparer.Ordinal).Count());
        Assert.All(files, file =>
        {
            Assert.EndsWith(".pdf", file.GetProperty("path").GetString()!, StringComparison.Ordinal);
            Assert.True(file.GetProperty("bytes").GetInt64() > 0);
            Assert.StartsWith("sha256:", file.GetProperty("hash").GetString()!, StringComparison.Ordinal);
        });
        Assert.Equal(
            manifest.RootElement.GetProperty("snapshot").GetProperty("files").GetInt32(),
            actual.Files);
        Assert.Equal(
            manifest.RootElement.GetProperty("snapshot").GetProperty("hash").GetString(),
            actual.Hash);
        Assert.Equal(
            "sha256-length-prefixed-path-and-bytes",
            manifest.RootElement.GetProperty("snapshot").GetProperty("algorithm").GetString());
    }

    [Fact]
    public void SnapshotHashIncludesRawBomBytes()
    {
        byte[] plain = Encoding.UTF8.GetBytes("same text");
        byte[] withBom = [.. Encoding.UTF8.GetPreamble(), .. plain];

        Assert.NotEqual(
            Emit.SnapshotHash(new Dictionary<string, byte[]> { ["entry.md"] = plain }).Hash,
            Emit.SnapshotHash(new Dictionary<string, byte[]> { ["entry.md"] = withBom }).Hash);
    }

    [Fact]
    public void SnapshotHashFramesPathsAndContentsWithoutBoundaryCollisions()
    {
        var first = new Dictionary<string, byte[]>
        {
            ["a"] = Encoding.UTF8.GetBytes("bc"),
            ["d"] = Encoding.UTF8.GetBytes("e"),
        };
        var second = new Dictionary<string, byte[]>
        {
            ["a"] = Encoding.UTF8.GetBytes("b"),
            ["cd"] = Encoding.UTF8.GetBytes("e"),
        };

        Assert.NotEqual(Emit.SnapshotHash(first).Hash, Emit.SnapshotHash(second).Hash);
    }

    private static (int Files, string Hash) IndependentSnapshotHash(string root)
    {
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetFileName(path), "UPSTREAM.md", StringComparison.Ordinal))
            .Where(path => string.Equals(Path.GetExtension(path), ".json", StringComparison.Ordinal)
                || string.Equals(Path.GetExtension(path), ".md", StringComparison.Ordinal))
            .Select(path => (
                Path: Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'),
                Bytes: File.ReadAllBytes(path)))
            .Where(file => !string.Equals(file.Path, "sources.manifest.json", StringComparison.Ordinal))
            .OrderBy(file => file.Path, StringComparer.Ordinal)
            .ToList();
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[sizeof(long)];
        foreach (var file in files)
        {
            byte[] path = Encoding.UTF8.GetBytes(file.Path);
            BinaryPrimitives.WriteInt64BigEndian(length, path.LongLength);
            digest.AppendData(length);
            digest.AppendData(path);
            BinaryPrimitives.WriteInt64BigEndian(length, file.Bytes.LongLength);
            digest.AppendData(length);
            digest.AppendData(file.Bytes);
        }

        return (
            files.Count,
            "sha256:" + Convert.ToHexString(digest.GetHashAndReset()).ToLowerInvariant());
    }
}
