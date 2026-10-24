using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class WreckingRecipeAdmissionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("unknown")]
    [InlineData("missing")]
    [InlineData("wrong-active-type")]
    [InlineData("invalid-active")]
    [InlineData("empty-groups")]
    public void MalformedGroupDeclarationsFailInsteadOfFallingBackToOneVillain(string corruption)
    {
        var root = WreckingRecipeFixture.Document();
        var recipe = root["campaigns"]!["wrecking_fixture"]!["encounter_groups"]!.AsObject();
        switch (corruption)
        {
            case "unknown": recipe["implicit_owner"] = true; break;
            case "missing": recipe.Remove("active_group"); break;
            case "wrong-active-type": recipe["active_group"] = "first"; break;
            case "invalid-active": recipe["active_group"] = 4; break;
            case "empty-groups": recipe["groups"] = new JsonArray(); break;
        }
        Assert.Throws<JsonException>(() => SetupCatalog.Parse(root.ToJsonString()));
    }

    [Theory]
    [InlineData("stage-order")]
    [InlineData("foreign-scheme")]
    [InlineData("short-deck")]
    [InlineData("extra-encounter-set")]
    public void InvalidPhysicalCompositionIsRefusedBeforeADealPlanIsPublished(string corruption)
    {
        var root = WreckingRecipeFixture.Document();
        var campaign = root["campaigns"]!["wrecking_fixture"]!;
        var first = campaign["encounter_groups"]!["groups"]![0]!;
        switch (corruption)
        {
            case "stage-order": campaign["villain"]![0] = "07003"; break;
            case "foreign-scheme": first["scheme"] = "01002"; break;
            case "short-deck": first["encounters"]!.AsArray().RemoveAt(0); break;
            case "extra-encounter-set": campaign["encounter_sets"]!.AsArray().Add("standard"); break;
        }
        var setup = SetupCatalog.Parse(root.ToJsonString());
        Assert.Throws<ArgumentException>(() => ScenarioDealer.Prepare(setup, new("wrecking_fixture", ["spider_man"]), Cards));
    }

    [Fact]
    public void LinkedCreationsInsideAGroupAreRefusedRatherThanMisbound()
    {
        // Synthetic future content isolates allocation safety; Wrecking Crew has no linked group cards.
        var document = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")))!;
        var linked = document["cards"]!.AsArray().Single(card => card!["card_id"]!.GetValue<string>() == "01002")!;
        linked["linked_to"] = new JsonArray("07005");
        var facts = CardCatalog.Parse(document.ToJsonString());
        var error = Assert.Throws<ArgumentException>(() => ScenarioDealer.Prepare(
            WreckingRecipeFixture.Catalog(), new("wrecking_fixture", ["spider_man"]), facts));
        Assert.Contains("blueprint allocation", error.Message);
    }

    [Fact]
    public void ACallerCannotAddAModularSetToThePrintedFourDeckComposition()
    {
        // pack:mc03:adjustable-difficulty: "The Wrecking Crew does not use other encounter sets".
        var setup = WreckingRecipeFixture.Catalog();
        Assert.Throws<ArgumentException>(() => ScenarioDealer.Prepare(setup,
            new("wrecking_fixture", ["spider_man"], ["bomb_scare"]), Cards));
    }

    [Theory]
    [InlineData("07004")]
    [InlineData("07005")]
    public void AdmissionIncludesEveryGroupSchemeAndDeckFace(string missing)
    {
        // This artificial complete set isolates recipe admission; it does not mark card abilities implemented.
        var setup = WreckingRecipeFixture.Catalog();
        var artificial = Cards.Faces.ToHashSet(StringComparer.Ordinal);
        SetupCatalogAdmission.Validate(setup, Cards, artificial);
        artificial.Remove(missing);
        var error = Assert.Throws<ArgumentException>(() => SetupCatalogAdmission.Validate(setup, Cards, artificial));
        Assert.Contains(missing, error.Message);
    }
}
