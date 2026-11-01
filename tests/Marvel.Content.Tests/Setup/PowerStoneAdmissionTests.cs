using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

// Power Stone (16149) is a one-card modular set within The Galaxy's Most Wanted (ruling
// 408decdf6cd8c4b5). Ship Command (16142–16148), the other modular set of its packet, stays
// closed: the Milano's "resource for any player" lets one seat spend another seat's card.
public sealed class PowerStoneAdmissionTests
{
    private const string Face = "16149";
    private static readonly string[] ShipCommand = ["16142", "16143", "16144", "16145", "16146", "16147", "16148"];
    private static readonly string SetupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
    private static readonly string CardJson = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
    private static readonly CardCatalog Cards = CardCatalog.Parse(CardJson);

    [Fact]
    public void ThePowerStoneSetOpensAsAModularAndShipCommandStaysClosed()
    {
        var setup = SetupCatalog.Parse(SetupJson);
        Assert.Equal(EncounterSetRole.Modular, setup.EncounterRole("power_stone"));
        Assert.Equal([Face], setup.EncounterSet("power_stone"));
        Assert.Equal(EncounterSetRole.Modular, setup.EncounterRole("ship_command"));
        var result = RuntimeProductAdmission.Discover(setup, Cards, AuthoredCards.Book.Authored);
        Assert.Contains(Face, result.Playable);
        Assert.Contains("power_stone", result.Setup.EncounterSetNames);
        Assert.All(ShipCommand, face => Assert.Contains(face, AuthoredCards.Book.Authored));
        Assert.All(ShipCommand, face => Assert.DoesNotContain(face, result.Playable));
        Assert.DoesNotContain("ship_command", result.Setup.EncounterSetNames);
    }

    [Fact]
    public void AMissingAuthoredFaceClosesTheSetAndPreservesOtherProducts()
    {
        var authored = AuthoredCards.Book.Authored.Where(value => value != Face).ToHashSet(StringComparer.Ordinal);
        AssertClosed(SetupCatalog.Parse(SetupJson), Cards, authored);
    }

    [Fact]
    public void AMissingCatalogFaceClosesTheSet()
    {
        var json = JsonNode.Parse(CardJson)!;
        var rows = json["cards"]!.AsArray();
        rows.Remove(rows.Single(row => row!["card_id"]!.GetValue<string>() == Face));
        AssertClosed(SetupCatalog.Parse(SetupJson), CardCatalog.Parse(json.ToJsonString()), AuthoredCards.Book.Authored);
    }

    [Fact]
    public void AFaceFromAnotherPackClosesTheSet()
    {
        var json = JsonNode.Parse(CardJson)!;
        json["cards"]!.AsArray().Single(row => row!["card_id"]!.GetValue<string>() == Face)!["pack"] = "core";
        Assert.False(StandaloneProductAdmission.PowerStone(SetupCatalog.Parse(SetupJson),
            CardCatalog.Parse(json.ToJsonString()), AuthoredCards.Book.Authored));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("extra")]
    [InlineData("foreign")]
    [InlineData("empty")]
    [InlineData("role")]
    public void AWrongOrMissingRecipeClosesTheSet(string change)
    {
        var json = JsonNode.Parse(SetupJson)!;
        var sets = json["encounter_sets"]!.AsObject();
        var recipe = sets["power_stone"]!;
        var cards = recipe["encounters"]!.AsArray();
        switch (change)
        {
            case "missing": sets.Remove("power_stone"); break;
            case "extra": cards.Add(Face); break;
            case "foreign": recipe["encounters"] = new JsonArray("01109"); break;
            case "empty": cards.Clear(); break;
            case "role": recipe["role"] = "scenario"; break;
        }
        var setup = SetupCatalog.Parse(json.ToJsonString());
        Assert.False(StandaloneProductAdmission.PowerStone(setup, Cards, AuthoredCards.Book.Authored));
        Assert.DoesNotContain(Face, RuntimeProductAdmission.Discover(setup, Cards, AuthoredCards.Book.Authored).Playable);
    }

    private static void AssertClosed(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored)
    {
        var baseline = RuntimeProductAdmission.Discover(SetupCatalog.Parse(SetupJson), Cards, AuthoredCards.Book.Authored);
        var result = RuntimeProductAdmission.Discover(setup, cards, authored);
        Assert.DoesNotContain("power_stone", result.Setup.EncounterSetNames);
        Assert.DoesNotContain(Face, result.Playable);
        Assert.Equal(baseline.Playable.Where(face => face != Face).Order(StringComparer.Ordinal),
            result.Playable.Order(StringComparer.Ordinal));
        Assert.Equal(baseline.Setup.HeroNames, result.Setup.HeroNames);
        Assert.Equal(baseline.Setup.EncounterSetNames.Where(key => key != "power_stone"), result.Setup.EncounterSetNames);
    }
}
