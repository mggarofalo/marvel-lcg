using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class StandaloneProductBoundaryTests
{
    [Fact]
    public void LegalAlternateStarterCannotReplaceVerifiedPublishedStarter()
    {
        var json = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")))!;
        json["heroes"]!["captain_america"]!["player_deck"]![0] = "03016";
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var result = RuntimeProductAdmission.Discover(SetupCatalog.Parse(json.ToJsonString()), cards, AuthoredCards.Book.Authored);
        Assert.DoesNotContain("captain_america", result.Setup.HeroNames);
    }

    [Theory]
    [InlineData("obligations")]
    [InlineData("nemesis_set")]
    public void IncompleteCaptainAmericaRecipeCannotAdmitProduct(string field)
    {
        var json = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")))!;
        json["heroes"]!["captain_america"]![field] = new JsonArray();
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var result = RuntimeProductAdmission.Discover(SetupCatalog.Parse(json.ToJsonString()), cards, AuthoredCards.Book.Authored);
        Assert.DoesNotContain("captain_america", result.Setup.HeroNames);
    }

    [Fact]
    public void DuplicateStandardRecipeCannotSupplyMissingExpertMode()
    {
        var json = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")))!;
        json["campaigns"]!["wrecking_crew_expert"] = json["campaigns"]!["wrecking_crew_standard"]!.DeepClone();
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var result = RuntimeProductAdmission.Discover(SetupCatalog.Parse(json.ToJsonString()), cards, AuthoredCards.Book.Authored);
        Assert.DoesNotContain("wrecking_crew_expert", result.Setup.CampaignNames);
    }
}
