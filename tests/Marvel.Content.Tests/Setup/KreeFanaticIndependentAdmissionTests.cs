using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class KreeFanaticIndependentAdmissionTests
{
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
    private static readonly string CardJson = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
    private static readonly string[] Faces = ["90001", "90002", "90003", "90004", "90005"];

    [Theory]
    [InlineData("90001")]
    [InlineData("90002")]
    [InlineData("90003")]
    [InlineData("90004")]
    [InlineData("90005")]
    public void LosingOnePhysicalCopyOfAnyFaceClosesAdmission(string face)
    {
        // Authority: numbered FFG PDF cards 1–7 specify quantities 1/1/1/2/2.
        var cards = CardCatalog.Parse(CardJson);
        Assert.True(StandaloneProductAdmission.KreeFanatic(SetupCatalog.Parse(SetupJson), cards, AuthoredCards.Book.Authored));
        var setup = JsonNode.Parse(SetupJson)!;
        var copies = setup["encounter_sets"]!["kree_fanatic"]!["encounters"]!.AsArray();
        copies.Remove(copies.First(item => item!.GetValue<string>() == face));
        Assert.False(StandaloneProductAdmission.KreeFanatic(SetupCatalog.Parse(setup.ToJsonString()), cards, AuthoredCards.Book.Authored));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EntirelyAbsentOrUnexpectedCatalogInventoryClosesOnlyThisProduct(bool unexpected)
    {
        var catalog = JsonNode.Parse(CardJson)!;
        var rows = catalog["cards"]!.AsArray();
        if (unexpected)
        {
            var extra = rows.Single(row => row!["card_id"]!.GetValue<string>() == "90001")!.DeepClone();
            extra["card_id"] = "90006";
            rows.Add(extra);
        }
        else
        {
            foreach (var row in rows.Where(row => row!["pack"]!.GetValue<string>() == "ron").ToArray())
                rows.Remove(row);
        }
        var baseline = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), CardCatalog.Parse(CardJson), AuthoredCards.Book.Authored);
        var result = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), CardCatalog.Parse(catalog.ToJsonString()), AuthoredCards.Book.Authored);
        Assert.DoesNotContain("kree_fanatic", result.Setup.EncounterSetNames);
        Assert.Equal(baseline.Playable.Except(Faces).Order(StringComparer.Ordinal), result.Playable.Order(StringComparer.Ordinal));
        Assert.Equal(baseline.Setup.CampaignNames, result.Setup.CampaignNames);
        Assert.Equal(baseline.Setup.HeroNames, result.Setup.HeroNames);
        Assert.NotNull(result.Campaign);
    }
}
