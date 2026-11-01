using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class AntManInventoryAdmissionTests
{
    private static readonly string CardJson = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
    private static readonly CardCatalog Cards = CardCatalog.Parse(CardJson);

    // Reviewed against the generated catalog and pinned Ant-Man catalog.
    // All three identity faces count; physical copy quantities are a separate setup contract.
    public static TheoryData<string> PrintedFaces => new()
    {
        "12001a", "12001b", "12001c", "12002", "12003", "12004", "12005", "12006", "12007",
        "12008", "12009", "12010", "12011", "12012", "12013", "12014", "12015",
        "12016", "12017", "12018", "12019", "12020", "12021", "12022", "12023",
        "12024", "12025", "12026", "12027", "12028", "12029", "12030", "12031",
        "12032", "12033",
    };

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void MissingAuthoredFaceClosesRuntimePackAndPreservesOtherProducts(string face)
    {
        var authored = AuthoredCards.Book.Authored.Where(id => id != face).ToHashSet(StringComparer.Ordinal);
        var admitted = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, authored);

        Assert.DoesNotContain("ant_man", admitted.Setup.HeroNames);
        Assert.DoesNotContain(admitted.Playable, id => Cards.Pack(id) == "ant");
        Assert.Equal(17, admitted.Setup.HeroNames.Count);
        Assert.Contains("spider_man", admitted.Setup.HeroNames);
        Assert.Contains("captain_america", admitted.Setup.HeroNames);
        Assert.Contains("hawkeye", admitted.Setup.HeroNames);
        Assert.Contains("wrecking_crew_standard", admitted.Setup.CampaignNames);
        Assert.Contains("mutagen_formula", admitted.Setup.CampaignNames);
        Assert.NotNull(admitted.Campaign);
    }

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void MissingCatalogFaceCannotSatisfyInventoryEvenWhenAuthored(string face)
    {
        var json = JsonNode.Parse(CardJson)!;
        var rows = json["cards"]!.AsArray();
        rows.Remove(rows.Single(row => row!["card_id"]!.GetValue<string>() == face));
        var cards = CardCatalog.Parse(json.ToJsonString());

        Assert.False(StandaloneProductAdmission.AntMan(SetupCatalog.Parse(SetupJson), cards,
            AuthoredCards.Book.Authored));
    }

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void ChangedProductMembershipCannotSatisfyInventoryEvenWhenAuthored(string face)
    {
        var json = JsonNode.Parse(CardJson)!;
        json["cards"]!.AsArray().Single(row => row!["card_id"]!.GetValue<string>() == face)!["pack"] = "unreviewed";
        var cards = CardCatalog.Parse(json.ToJsonString());

        Assert.False(StandaloneProductAdmission.AntMan(SetupCatalog.Parse(SetupJson), cards,
            AuthoredCards.Book.Authored));
    }
}
