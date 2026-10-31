using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class BlackWidowInventoryAdmissionTests
{
    private static readonly string CardJson = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
    private static readonly CardCatalog Cards = CardCatalog.Parse(CardJson);

    // Reviewed against the generated catalog and pinned bkw/bkw_encounter snapshot.
    // Both identity faces count; physical copy quantities are a separate setup contract.
    public static TheoryData<string> PrintedFaces => new()
    {
        "08001a", "08001b", "08002", "08003", "08004", "08005", "08006", "08007", "08008",
        "08009", "08010", "08011", "08012", "08013", "08014", "08015", "08016", "08017",
        "08018", "08019", "08020", "08021", "08022", "08023", "08024", "08025", "08026",
        "08027", "08028", "08029", "08030", "08031", "08032", "08033",
    };

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void MissingAuthoredFaceClosesRuntimePackAndPreservesOtherProducts(string face)
    {
        var authored = AuthoredCards.Book.Authored.Where(id => id != face).ToHashSet(StringComparer.Ordinal);
        var admitted = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, authored);

        Assert.DoesNotContain("black_widow", admitted.Setup.HeroNames);
        Assert.DoesNotContain(admitted.Playable, id => Cards.Pack(id) == "bkw");
        Assert.Equal(15, admitted.Setup.HeroNames.Count);
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

        Assert.False(StandaloneProductAdmission.BlackWidow(SetupCatalog.Parse(SetupJson), cards,
            AuthoredCards.Book.Authored));
    }

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void ChangedProductMembershipCannotSatisfyInventoryEvenWhenAuthored(string face)
    {
        var json = JsonNode.Parse(CardJson)!;
        json["cards"]!.AsArray().Single(row => row!["card_id"]!.GetValue<string>() == face)!["pack"] = "drs";
        var cards = CardCatalog.Parse(json.ToJsonString());

        Assert.False(StandaloneProductAdmission.BlackWidow(SetupCatalog.Parse(SetupJson), cards,
            AuthoredCards.Book.Authored));
    }
}
