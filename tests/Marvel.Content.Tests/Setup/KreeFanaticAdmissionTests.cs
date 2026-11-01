using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class KreeFanaticAdmissionTests
{
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
    private static readonly string CardJson = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
    private static readonly CardCatalog Cards = CardCatalog.Parse(CardJson);
    private static readonly string[] Faces = ["90001", "90002", "90003", "90004", "90005"];
    public static IEnumerable<object[]> RequiredFaces() => Faces.Select(face => new object[] { face });

    [Fact]
    public void CompletePrintedSetAdmitsExactlySevenPhysicalCards()
    {
        // Authority: pinned FFG ronan_modular_set.pdf, numbered cards 1–7;
        // 90001–90003 have one copy each and 90004–90005 have two each.
        var setup = SetupCatalog.Parse(SetupJson);
        Assert.Equal("Kree Fanatic", setup.EncounterSetDisplayName("kree_fanatic"));
        Assert.Equal(EncounterSetRole.Modular, setup.EncounterRole("kree_fanatic"));
        Assert.Equal(["90001", "90002", "90003", "90004", "90004", "90005", "90005"], setup.EncounterSet("kree_fanatic"));
        var result = RuntimeProductAdmission.Discover(setup, Cards, AuthoredCards.Book.Authored);
        Assert.Equal(Faces, result.Playable.Where(face => Cards.Pack(face) == "ron").Order(StringComparer.Ordinal));
        Assert.All(Faces, face => Assert.Equal("kree_fanatic", Cards.EncounterSet(face)));
        Assert.Contains("kree_fanatic", result.Setup.EncounterSetNames);
        Assert.DoesNotContain("ronan", result.Setup.CampaignNames);
        // Groot (16001a–16028) is the only open part of The Galaxy's Most Wanted.
        Assert.DoesNotContain(result.Playable, face => Cards.Pack(face) == "gmw" && string.CompareOrdinal(face, "16029") >= 0);
    }

    [Theory]
    [MemberData(nameof(RequiredFaces))]
    public void EveryMissingAuthoredFaceClosesWholeSetAndPreservesOtherProducts(string face)
    {
        var authored = AuthoredCards.Book.Authored.Where(value => value != face).ToHashSet(StringComparer.Ordinal);
        AssertClosed(SetupCatalog.Parse(SetupJson), Cards, authored);
    }

    [Theory]
    [MemberData(nameof(RequiredFaces))]
    public void EveryMissingCatalogFaceClosesWholeSet(string face)
    {
        var json = JsonNode.Parse(CardJson)!;
        var rows = json["cards"]!.AsArray();
        rows.Remove(rows.Single(row => row!["card_id"]!.GetValue<string>() == face));
        AssertClosed(SetupCatalog.Parse(SetupJson), CardCatalog.Parse(json.ToJsonString()), AuthoredCards.Book.Authored);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("short")]
    [InlineData("extra")]
    [InlineData("same-count")]
    [InlineData("foreign")]
    [InlineData("empty")]
    [InlineData("role")]
    public void WrongOrMissingRecipeRejectsWholeSet(string change)
    {
        var json = JsonNode.Parse(SetupJson)!;
        var sets = json["encounter_sets"]!.AsObject();
        var recipe = sets["kree_fanatic"]!;
        var cards = recipe["encounters"]!.AsArray();
        switch (change)
        {
            case "missing": sets.Remove("kree_fanatic"); break;
            case "short": cards.RemoveAt(0); break;
            case "extra": cards.Add("90001"); break;
            case "same-count": cards[0] = "90004"; break;
            case "foreign": recipe["encounters"] = new JsonArray("01109"); break;
            case "empty": cards.Clear(); break;
            case "role": recipe["role"] = "scenario"; break;
        }
        AssertClosed(SetupCatalog.Parse(json.ToJsonString()), Cards, AuthoredCards.Book.Authored);
    }

    [Fact]
    public void ReorderingPhysicalCopiesPreservesAdmission()
    {
        var json = JsonNode.Parse(SetupJson)!;
        json["encounter_sets"]!["kree_fanatic"]!["encounters"] =
            new JsonArray("90005", "90004", "90003", "90002", "90001", "90005", "90004");
        Assert.True(StandaloneProductAdmission.KreeFanatic(SetupCatalog.Parse(json.ToJsonString()), Cards, AuthoredCards.Book.Authored));
    }

    private static void AssertClosed(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored)
    {
        var baseline = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, AuthoredCards.Book.Authored);
        var result = RuntimeProductAdmission.Discover(setup, cards, authored);
        Assert.DoesNotContain("kree_fanatic", result.Setup.EncounterSetNames);
        Assert.All(Faces, face => Assert.DoesNotContain(face, result.Playable));
        Assert.Equal(baseline.Playable.Except(Faces).Order(StringComparer.Ordinal), result.Playable.Order(StringComparer.Ordinal));
        Assert.Equal(baseline.Setup.CampaignNames, result.Setup.CampaignNames);
        Assert.Equal(baseline.Setup.HeroNames, result.Setup.HeroNames);
        Assert.Equal(baseline.Setup.EncounterSetNames.Where(key => key != "kree_fanatic"), result.Setup.EncounterSetNames);
        Assert.NotNull(result.Campaign);
    }
}
