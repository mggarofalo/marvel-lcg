using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class DoctorStrangeInventoryAdmissionTests
{
    private static readonly string CardJson = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
    private static readonly CardCatalog Cards = CardCatalog.Parse(CardJson);

    // Reviewed against the generated catalog and pinned drs/drs_encounter snapshot.
    // Both identity faces count; physical copy quantities are a separate setup contract.
    public static TheoryData<string> PrintedFaces => new()
    {
        "09001a", "09001b", "09002", "09003", "09004", "09005", "09006", "09007", "09008",
        "09009", "09010", "09011", "09012", "09013", "09014", "09015", "09016", "09017",
        "09018", "09019", "09020", "09021", "09022", "09023", "09024", "09025", "09026",
        "09027", "09028", "09029", "09030", "09031", "09032", "09033", "09034", "09035",
        "09036", "09037", "09038", "09039",
    };

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void MissingAuthoredFaceClosesRuntimePackAndPreservesOtherProducts(string face)
    {
        var authored = AuthoredCards.Book.Authored.Where(id => id != face).ToHashSet(StringComparer.Ordinal);
        var admitted = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, authored);

        Assert.DoesNotContain("doctor_strange", admitted.Setup.HeroNames);
        Assert.DoesNotContain(admitted.Playable, id => Cards.Pack(id) == "drs");
        Assert.Equal(16, admitted.Setup.HeroNames.Count);
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

        Assert.False(StandaloneProductAdmission.DoctorStrange(SetupCatalog.Parse(SetupJson), cards,
            AuthoredCards.Book.Authored));
    }

    [Theory]
    [MemberData(nameof(PrintedFaces))]
    public void ChangedProductMembershipCannotSatisfyInventoryEvenWhenAuthored(string face)
    {
        var json = JsonNode.Parse(CardJson)!;
        json["cards"]!.AsArray().Single(row => row!["card_id"]!.GetValue<string>() == face)!["pack"] = "hlk";
        var cards = CardCatalog.Parse(json.ToJsonString());

        Assert.False(StandaloneProductAdmission.DoctorStrange(SetupCatalog.Parse(SetupJson), cards,
            AuthoredCards.Book.Authored));
    }
}
