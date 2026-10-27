using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class MsMarvelAdmissionIndependentTests
{
    private static readonly string CardJson = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
    private static readonly CardCatalog Cards = CardCatalog.Parse(CardJson);

    // Reviewed against the generated catalog and pinned msm/msm_encounter snapshot.
    // Both identity faces count; physical copy quantities are a separate setup contract.
    public static TheoryData<string> PrintedFaces => new()
    {
        "05001a", "05001b", "05002", "05003", "05004", "05005", "05006", "05007", "05008",
        "05009", "05010", "05011", "05012", "05013", "05014", "05015", "05016", "05017",
        "05018", "05019", "05020", "05021", "05022", "05023", "05024", "05025", "05026",
        "05027", "05028", "05029", "05030", "05031", "05032", "05033",
    };

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void MissingAuthoredFaceClosesRuntimePackAndPreservesOtherProducts(string face)
    {
        var authored = AuthoredCards.Book.Authored.Where(id => id != face).ToHashSet(StringComparer.Ordinal);
        var admitted = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, authored);

        Assert.DoesNotContain("ms_marvel", admitted.Setup.HeroNames);
        Assert.DoesNotContain(admitted.Playable, id => Cards.Pack(id) == "msm");
        Assert.Equal(11, admitted.Setup.HeroNames.Count);
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

        Assert.False(StandaloneProductAdmission.MsMarvel(SetupCatalog.Parse(SetupJson), cards,
            AuthoredCards.Book.Authored));
    }

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void ChangedProductMembershipCannotSatisfyInventoryEvenWhenAuthored(string face)
    {
        var json = JsonNode.Parse(CardJson)!;
        json["cards"]!.AsArray().Single(row => row!["card_id"]!.GetValue<string>() == face)!["pack"] = "thor";
        var cards = CardCatalog.Parse(json.ToJsonString());

        Assert.False(StandaloneProductAdmission.MsMarvel(SetupCatalog.Parse(SetupJson), cards,
            AuthoredCards.Book.Authored));
    }
}
