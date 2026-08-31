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
        IReadOnlyDictionary<string, string> tree = Emit.ReadTree(root);
        var actual = Emit.SnapshotHash(tree);

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
    }
}
