using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Marvel.Rulings.Harvest;
using Marvel.Tests;
using Xunit;
using RulingsHarvest = Marvel.Rulings.Harvest.Harvest;

namespace Marvel.Rules.Tests.Rules;

public sealed class RulingsHarvestTests
{
    private static readonly Page Chronological = new(
        "post-rrg-1-7",
        "post-rrg-1-7.html",
        "hallofheroeslcg.com/latest-ffg-rulings-post-rrg-1-7/",
        "1.7-1.8",
        PageShape.Chronological);

    [Fact]
    public void ChronologicalGroupsCarryTheirMonthAndNormalizedAttribution()
    {
        var rulings = RulingsHarvest.Read(PageWith(
            """
            <h2>March, 2026</h2>
            <blockquote><p>How are minion attacks ordered?</p></blockquote>
            <p>In the order of your choice.</p>
            <p>-Alex – March 5, 2026</p>
            """), Chronological);

        Ruling ruling = Assert.Single(rulings);
        Assert.Equal("2026-03", ruling.Observed);
        Assert.Equal("Alex Werner (FFG Game Rules Specialist)", ruling.Source);
        Assert.Equal("rules", ruling.Kind);
        Assert.Empty(ruling.Cards);
    }

    [Fact]
    public void CompendiumAndChronologicalPagesUseTheSameQuestionAnswerShape()
    {
        string html = PageWith(
            """
            <h2>Core Set</h2>
            <blockquote><p>A question?</p></blockquote>
            <p>An answer.</p>
            <p>-[Marvel Champions LCG Designer] Caleb Grace</p>
            """);
        var compendium = Chronological with
        {
            Name = "official-ffg-rulings",
            Via = "hallofheroeslcg.com/official-ffg-rulings/",
            RulesReferenceScope = "pre-1.5",
            Shape = PageShape.Compendium,
        };

        Ruling ruling = Assert.Single(RulingsHarvest.Read(html, compendium));
        Assert.Equal("Caleb Grace (Marvel Champions LCG designer)", ruling.Source);
        Assert.Null(ruling.Observed);
    }

    [Fact]
    public void OneAttributionCanCloseSeveralQuestionAnswerBlocks()
    {
        var rulings = RulingsHarvest.Read(PageWith(
            """
            <h2>March, 2026</h2>
            <blockquote><p>First question?</p></blockquote>
            <p>First answer.</p>
            <blockquote><p>Second question?</p></blockquote>
            <p>Second answer.</p>
            <p>-Alex – March 5, 2026</p>
            """), Chronological);

        Assert.Equal(["First question?", "Second question?"], rulings.Select(ruling => ruling.Question));
    }

    [Fact]
    public void ABlockquoteCanWrapANestedListAnswer()
    {
        var rulings = RulingsHarvest.Read(PageWith(
            """
            <h2>March, 2026</h2>
            <blockquote><p>A question?</p></blockquote>
            <blockquote><ul><li>Outer answer.<ul><li>Nested qualification.</li></ul></li><li>Second answer.</li></ul></blockquote>
            <p>-Alex – March 5, 2026</p>
            """), Chronological);

        Ruling ruling = Assert.Single(rulings);
        Assert.Equal("A question?", ruling.Question);
        Assert.Equal("Outer answer. Nested qualification. Second answer.", ruling.Answer);
    }

    [Fact]
    public void ABareParagraphQuestionCanPrecedeABlockquoteAnswer()
    {
        var rulings = RulingsHarvest.Read(PageWith(
            """
            <h2>March, 2026</h2>
            <p>A bare question?</p>
            <blockquote><p>First answer paragraph.</p><p>Second answer paragraph.</p></blockquote>
            <p>-Alex – March 5, 2026</p>
            """), Chronological);

        Ruling ruling = Assert.Single(rulings);
        Assert.Equal("A bare question?", ruling.Question);
        Assert.Equal("First answer paragraph. Second answer paragraph.", ruling.Answer);
    }

    [Fact]
    public void TheAuditedFebruary2026BylineTypoUsesItsChronologicalSection()
    {
        var rulings = RulingsHarvest.Read(PageWith(
            """
            <h2>February, 2026</h2>
            <blockquote><p>A question?</p></blockquote>
            <p>An answer.</p>
            <p>-Alex – February 20, 2025</p>
            """), Chronological);

        Assert.Equal("2026-02", Assert.Single(rulings).Observed);
    }

    [Fact]
    public void AnUnauditedBackwardBylineDateFailsTheHarvest()
    {
        string html = PageWith(
            """
            <h2>March, 2026</h2>
            <blockquote><p>A question?</p></blockquote>
            <p>An answer.</p>
            <p>-Alex – February 20, 2026</p>
            """);

        Assert.Throws<InvalidDataException>(() => RulingsHarvest.Read(html, Chronological));
    }

    [Fact]
    public void ReorderingDoesNotMoveAnIdButRevisingAnAnswerMovesTheHash()
    {
        var first = Ruling.Create("A question?", "First answer.", "A source", Chronological, "March, 2026", "2026-03", []);
        var revised = Ruling.Create("A question?", "Revised answer.", "A source", Chronological, "March, 2026", "2026-03", []);

        Assert.Equal(first.Id, revised.Id);
        Assert.NotEqual(first.Hash, revised.Hash);
        Assert.StartsWith("ruling:", first.Id, StringComparison.Ordinal);
        Assert.StartsWith("sha256:", first.Hash, StringComparison.Ordinal);
    }

    [Fact]
    public void TheIndexAlwaysUsesTheRepositorysLfWireFormat()
    {
        byte[] bytes = Emit.JsonBytes([], "2026-08-31");
        string json = Encoding.UTF8.GetString(bytes);

        Assert.False(bytes.AsSpan().StartsWith(new byte[] { 0xef, 0xbb, 0xbf }));
        Assert.DoesNotContain("\r", json, StringComparison.Ordinal);
        Assert.EndsWith("\n", json, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingInputsFailWriteButRemainOptionalForCandidateCheck()
    {
        string temporary = Path.Combine(Path.GetTempPath(), $"marvel-rulings-{Guid.NewGuid():N}");
        string output = temporary + "-output";
        Directory.CreateDirectory(temporary);
        try
        {
            Assert.Equal(1, RunTool("write", temporary, output, "2026-08-31"));
            Assert.Equal(0, RunTool("check", temporary));
            Assert.False(File.Exists(Path.Combine(output, "rulings.json")));
        }
        finally
        {
            Directory.Delete(temporary, true);
            if (Directory.Exists(output))
            {
                Directory.Delete(output, true);
            }
        }
    }

    [Fact]
    public void TheVendoredSnapshotHoldsEveryPageAndCardlessRulesRulings()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(
            RepositoryPaths.Dataset("rulings", "rulings.json")));
        var rulings = json.RootElement.GetProperty("rulings").EnumerateArray().ToList();

        Assert.Equal(1100, rulings.Count);
        Assert.Equal(rulings.Count, rulings.Select(ItemId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(4, rulings.Select(ItemVia).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(rulings, item =>
            item.GetProperty("question").GetString() == "How are minion attacks ordered?"
            && item.GetProperty("kind").GetString() == "rules"
            && item.GetProperty("cards").GetArrayLength() == 0);
        Assert.All(rulings, item =>
        {
            Assert.StartsWith("ruling:", ItemId(item), StringComparison.Ordinal);
            Assert.StartsWith("sha256:", item.GetProperty("hash").GetString()!, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("source").GetString()));
        });

        AssertPinned(rulings, "ruling:7dc8823b0fc2f0f2", "For the second part, no");
        AssertPinned(rulings, "ruling:214f4f7ab3c9c2f1", "Maximum Efficiency");
        AssertPinned(rulings, "ruling:95b74681b4b10f3e", "zero cards from 4 aspects");
        AssertPinned(rulings, "ruling:99935fc95e8b7290", "Steady identities would be given 2");
        AssertPinned(rulings, "ruling:7611b62cbe00068d", "other players can defend");
        AssertPinned(rulings, "ruling:c96a5a056c20901f", "removed from the game instead");
        JsonElement corrected = AssertPinned(rulings, "ruling:121d5b7b5dcec377", "Cruel Experiment");
        Assert.Equal("2026-02", corrected.GetProperty("observed").GetString());
    }

    [Fact]
    public void TheMachineReadableManifestPinsEveryVendoredPageByteForByte()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(
            RepositoryPaths.Dataset("rulings", "pages.manifest.json")));
        var files = manifest.RootElement.GetProperty("files").EnumerateArray().ToList();

        Assert.Equal(4, files.Count);
        Assert.All(files, file =>
        {
            string relative = file.GetProperty("path").GetString()!;
            byte[] bytes = File.ReadAllBytes(RepositoryPaths.Dataset(["rulings", .. relative.Split('/')]));
            string hash = "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            Assert.Equal(bytes.LongLength, file.GetProperty("bytes").GetInt64());
            Assert.Equal(hash, file.GetProperty("hash").GetString());
        });
    }

    private static string PageWith(string body) =>
        $"<html><div class=\"entry-content\">{body}<footer class=\"entry-footer\"></footer></div></html>";

    private static string ItemId(JsonElement item) => item.GetProperty("id").GetString()!;

    private static string ItemVia(JsonElement item) => item.GetProperty("via").GetString()!;

    private static JsonElement AssertPinned(
        IReadOnlyList<JsonElement> rulings,
        string id,
        string expectedText)
    {
        JsonElement ruling = Assert.Single(rulings, item => ItemId(item) == id);
        Assert.Contains(
            expectedText,
            ruling.GetProperty("answer").GetString()!,
            StringComparison.Ordinal);
        return ruling;
    }

    private static int RunTool(params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(typeof(Ruling).Assembly.Location);
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start)!;
        process.WaitForExit();
        return process.ExitCode;
    }
}
