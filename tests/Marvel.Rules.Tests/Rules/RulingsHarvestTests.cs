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
        string json = Emit.Json([], "2026-08-31");

        Assert.DoesNotContain("\r", json, StringComparison.Ordinal);
        Assert.EndsWith("\n", json, StringComparison.Ordinal);
    }

    [Fact]
    public void TheVendoredSnapshotHoldsEveryPageAndCardlessRulesRulings()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(
            RepositoryPaths.Dataset("rulings", "rulings.json")));
        var rulings = json.RootElement.GetProperty("rulings").EnumerateArray().ToList();

        Assert.True(rulings.Count > 900, $"expected the surveyed corpus, found {rulings.Count}");
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
    }

    private static string PageWith(string body) =>
        $"<html><div class=\"entry-content\">{body}<footer class=\"entry-footer\"></footer></div></html>";

    private static string ItemId(JsonElement item) => item.GetProperty("id").GetString()!;

    private static string ItemVia(JsonElement item) => item.GetProperty("via").GetString()!;
}
