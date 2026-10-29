using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class HulkInventoryAdmissionTests
{
    private static readonly string CardJson = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
    private static readonly CardCatalog Cards = CardCatalog.Parse(CardJson);

    // Reviewed against the generated catalog and pinned hlk/hlk_encounter snapshot.
    // Both identity faces count; physical copy quantities are a separate setup contract.
    public static TheoryData<string> PrintedFaces => new()
    {
        "10001a", "10001b", "10002", "10003", "10004", "10005", "10006", "10007",
        "10008", "10009", "10010", "10011", "10012", "10013", "10014", "10015",
        "10016", "10017", "10018", "10019", "10020", "10021", "10022", "10023",
        "10024", "10025", "10026", "10027", "10028", "10029", "10030", "10031",
        "10032",
    };

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void MissingAuthoredFaceClosesRuntimePackAndPreservesOtherProducts(string face)
    {
        var authored = AuthoredCards.Book.Authored.Where(id => id != face).ToHashSet(StringComparer.Ordinal);
        var admitted = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, authored);

        Assert.DoesNotContain("hulk", admitted.Setup.HeroNames);
        Assert.DoesNotContain(admitted.Playable, id => Cards.Pack(id) == "hlk");
        Assert.Equal(13, admitted.Setup.HeroNames.Count);
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

        Assert.False(StandaloneProductAdmission.Hulk(SetupCatalog.Parse(SetupJson), cards,
            AuthoredCards.Book.Authored));
    }

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void ChangedProductMembershipCannotSatisfyInventoryEvenWhenAuthored(string face)
    {
        var json = JsonNode.Parse(CardJson)!;
        json["cards"]!.AsArray().Single(row => row!["card_id"]!.GetValue<string>() == face)!["pack"] = "ant";
        var cards = CardCatalog.Parse(json.ToJsonString());

        Assert.False(StandaloneProductAdmission.Hulk(SetupCatalog.Parse(SetupJson), cards,
            AuthoredCards.Book.Authored));
    }
}
