using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class StandaloneProductAdmissionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));

    public static IEnumerable<object[]> RequiredFaces() => Cards.Faces
        .Where(face => Cards.Pack(face) is "cap" or "twc").Select(face => new object[] { face });

    public static IEnumerable<object[]> RequiredRecipes() => SetupCatalog.Parse(SetupJson).CampaignNames
        .Where(key => key.StartsWith("wrecking_crew_", StringComparison.Ordinal)).Select(key => new object[] { key });

    [Theory]
    [MemberData(nameof(RequiredFaces))]
    public void MissingAnyAuthoredFaceClosesItsEntireProduct(string missing)
    {
        var authored = AuthoredCards.Book.Authored.Where(face => face != missing).ToHashSet(StringComparer.Ordinal);
        var setup = SetupCatalog.Parse(SetupJson);
        Assert.Equal(Cards.Pack(missing) != "cap", StandaloneProductAdmission.CaptainAmerica(setup, Cards, authored));
        Assert.Equal(Cards.Pack(missing) != "twc", StandaloneProductAdmission.WreckingCrew(setup, Cards, authored));
    }

    [Theory]
    [MemberData(nameof(RequiredRecipes))]
    [InlineData("captain_america")]
    public void MissingAnyRequiredRecipeClosesItsEntireProduct(string missing)
    {
        var json = JsonNode.Parse(SetupJson)!;
        bool hero = missing == "captain_america";
        json[hero ? "heroes" : "campaigns"]!.AsObject().Remove(missing);
        var admitted = RuntimeProductAdmission.Discover(SetupCatalog.Parse(json.ToJsonString()), Cards, AuthoredCards.Book.Authored);
        Assert.DoesNotContain(admitted.Playable, face => Cards.Pack(face) == (hero ? "cap" : "twc"));
        Assert.DoesNotContain(admitted.Setup.CampaignNames, key => !hero && key.StartsWith("wrecking_crew_", StringComparison.Ordinal));
        Assert.Contains("hawkeye", admitted.Setup.HeroNames);
        Assert.NotNull(admitted.Campaign);
    }

    [Theory]
    [InlineData("cap", "03034")]
    [InlineData("twc", "07059")]
    public void EqualFaceCountCannotSubstituteAnUnreviewedPrintedFace(string pack, string replaced)
    {
        var json = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")))!;
        json["cards"]!.AsArray().Single(card => card!["card_id"]!.GetValue<string>() == replaced)!["card_id"] = "99999";
        var cards = CardCatalog.Parse(json.ToJsonString());
        var authored = AuthoredCards.Book.Authored.Append("99999").ToHashSet(StringComparer.Ordinal);
        var setup = SetupCatalog.Parse(SetupJson);
        Assert.False(pack == "cap" ? StandaloneProductAdmission.CaptainAmerica(setup, cards, authored)
            : StandaloneProductAdmission.WreckingCrew(setup, cards, authored));
    }
}
