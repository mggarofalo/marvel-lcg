using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

// Band of Badoon (16117–16121) is a modular set within The Galaxy's Most Wanted (ruling
// 408decdf6cd8c4b5). The Brotherhood of Badoon scenario stays closed: its encounter deck
// includes the withheld Ship Command set, and Drang (III) is unauthored.
public sealed class BandOfBadoonAdmissionTests
{
    private static readonly string[] Faces = ["16117", "16118", "16119", "16120", "16121"];
    private static readonly string[] Scenario =
        ["16058", "16059", "16061a", "16061b", "16062a", "16062b", "16063", "16064", "16065", "16066", "16067", "16068", "16069"];
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
    private static readonly string CardJson = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
    private static readonly CardCatalog Cards = CardCatalog.Parse(CardJson);

    [Fact]
    public void TheBandOfBadoonSetOpensAsAModularAndTheScenarioStaysClosed()
    {
        var setup = SetupCatalog.Parse(SetupJson);
        Assert.Equal(EncounterSetRole.Modular, setup.EncounterRole("band_of_badoon"));
        Assert.Equal(["16117", "16117", "16118", "16118", "16118", "16119", "16120", "16120", "16121", "16121"],
            setup.EncounterSet("band_of_badoon"));
        var result = RuntimeProductAdmission.Discover(setup, Cards, AuthoredCards.Book.Authored);
        Assert.All(Faces, face => Assert.Contains(face, result.Playable));
        Assert.Contains("band_of_badoon", result.Setup.EncounterSetNames);
        Assert.All(Scenario, face => Assert.Contains(face, AuthoredCards.Book.Authored));
        Assert.All(Scenario, face => Assert.DoesNotContain(face, result.Playable));
        Assert.DoesNotContain("16060", AuthoredCards.Book.Authored);
        Assert.DoesNotContain("brotherhood_of_badoon", result.Setup.CampaignNames);
        Assert.DoesNotContain("brotherhood_of_badoon_expert", result.Setup.CampaignNames);
    }

    [Theory]
    [InlineData("16117")]
    [InlineData("16121")]
    public void AMissingAuthoredFaceClosesTheSetAndPreservesOtherProducts(string face)
    {
        var authored = AuthoredCards.Book.Authored.Where(value => value != face).ToHashSet(StringComparer.Ordinal);
        AssertClosed(SetupCatalog.Parse(SetupJson), Cards, authored);
    }

    [Fact]
    public void AMissingCatalogFaceClosesTheSet()
    {
        var json = JsonNode.Parse(CardJson)!;
        var rows = json["cards"]!.AsArray();
        rows.Remove(rows.Single(row => row!["card_id"]!.GetValue<string>() == "16119"));
        AssertClosed(SetupCatalog.Parse(SetupJson), CardCatalog.Parse(json.ToJsonString()), AuthoredCards.Book.Authored);
    }

    [Fact]
    public void AFaceFromAnotherPackClosesTheSet()
    {
        var json = JsonNode.Parse(CardJson)!;
        json["cards"]!.AsArray().Single(row => row!["card_id"]!.GetValue<string>() == "16118")!["pack"] = "core";
        Assert.False(StandaloneProductAdmission.BandOfBadoon(SetupCatalog.Parse(SetupJson),
            CardCatalog.Parse(json.ToJsonString()), AuthoredCards.Book.Authored));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("extra")]
    [InlineData("short")]
    [InlineData("foreign")]
    [InlineData("role")]
    public void AWrongOrMissingRecipeClosesTheSet(string change)
    {
        var json = JsonNode.Parse(SetupJson)!;
        var sets = json["encounter_sets"]!.AsObject();
        var recipe = sets["band_of_badoon"]!;
        var cards = recipe["encounters"]!.AsArray();
        switch (change)
        {
            case "missing": sets.Remove("band_of_badoon"); break;
            case "extra": cards.Add("16118"); break;
            case "short": cards.RemoveAt(cards.Count - 1); break;
            case "foreign": cards[0] = "01109"; break;
            case "role": recipe["role"] = "scenario"; break;
        }
        var setup = SetupCatalog.Parse(json.ToJsonString());
        Assert.False(StandaloneProductAdmission.BandOfBadoon(setup, Cards, AuthoredCards.Book.Authored));
        var result = RuntimeProductAdmission.Discover(setup, Cards, AuthoredCards.Book.Authored);
        Assert.All(Faces, face => Assert.DoesNotContain(face, result.Playable));
    }

    private static void AssertClosed(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored)
    {
        var baseline = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, AuthoredCards.Book.Authored);
        var result = RuntimeProductAdmission.Discover(setup, cards, authored);
        Assert.DoesNotContain("band_of_badoon", result.Setup.EncounterSetNames);
        Assert.All(Faces, face => Assert.DoesNotContain(face, result.Playable));
        Assert.Equal(baseline.Playable.Where(face => !Faces.Contains(face)).Order(StringComparer.Ordinal),
            result.Playable.Order(StringComparer.Ordinal));
        Assert.Equal(baseline.Setup.HeroNames, result.Setup.HeroNames);
        Assert.Equal(baseline.Setup.EncounterSetNames.Where(key => key != "band_of_badoon"), result.Setup.EncounterSetNames);
    }
}
