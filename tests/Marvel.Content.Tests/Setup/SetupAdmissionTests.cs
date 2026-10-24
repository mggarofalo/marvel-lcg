using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Content.Setup;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class SetupAdmissionTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("\"true\"")]
    [InlineData("1")]
    [InlineData("[]")]
    public void ExpertRequiresAnExplicitBoolean(string value)
    {
        JsonNode data = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")))!;
        data["campaigns"]!["rhino"]!["expert"] = JsonNode.Parse(value);

        Assert.Throws<JsonException>(() => SetupCatalog.Parse(data.ToJsonString()));
    }

    [Fact]
    public void MissingExpertDoesNotSilentlySelectStandard()
    {
        JsonNode data = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")))!;
        data["campaigns"]!["rhino"]!.AsObject().Remove("expert");
        Assert.Throws<JsonException>(() => SetupCatalog.Parse(data.ToJsonString()));
    }

    [Rule("rr:appendix-ii-setup.step.1")]
    [Fact]
    public void IdentityOrientationUsesPrintedKindInsteadOfIdSuffix()
    {
        // "Each player selects one identity, placing their alter-ego side face up."
        var facts = CardCatalog.Parse("""
            {"cards":[{"card_id":"hero-b","type":"Hero"},
                      {"card_id":"civilian","type":"AlterEgo"}]}
            """);

        Assert.Equal("civilian,hero-b", IdentityFaces.StartingSpec("hero-b,civilian", facts));
        Assert.Throws<ArgumentException>(() => IdentityFaces.StartingSpec("hero-b", facts));
        Assert.Throws<ArgumentException>(() => IdentityFaces.StartingSpec("civilian", facts));
        Assert.Throws<ArgumentException>(() => IdentityFaces.StartingSpec("hero-b,hero-b,civilian", facts));
        Assert.Throws<ArgumentException>(() => IdentityFaces.StartingSpec("civilian,civilian", facts));
    }

    [Fact]
    public void AuthoredSetRolesAreIndependentOfIdentifierSpelling()
    {
        var facts = CardCatalog.Parse("""
            {"cards":[{"card_id":"card","type":"Minion","set":"standard_custom"}]}
            """);
        var setup = SetupCatalog.Parse("""
            {"campaigns":{},"heroes":{},"encounter_sets":{
              "custom":{"name":"Custom","role":"modular","encounters":["card"]},
              "scenario":{"name":"Scenario","role":"scenario","encounters":["card"]}
            }}
            """);

        Assert.True(ModularEncounterSets.IsModular(setup, facts, "custom"));
        Assert.False(ModularEncounterSets.IsModular(setup, facts, "scenario"));
    }

    [Fact]
    public void CompleteCoreContentIsAdmittedAndMissingExecutionIsRejected()
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var program = AbilityLowering.Book(AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));

        setup = RuntimeProductAdmission.Discover(setup, cards, program.Authored).Setup.WithoutPack(cards, "trors");
        SetupCatalogAdmission.Validate(setup, cards, program.Authored);
        Assert.Throws<ArgumentException>(() => SetupCatalogAdmission.Validate(setup, cards, program.Authored.Remove("01001a")));
        Assert.Throws<ArgumentException>(() => Dealer.DealOrder(setup, "rhino", ["spider_man"],
            ["bomb_scare", "bomb_scare"], cards));
    }
    [Fact]
    public void NemesisAssociationIsAuthoredRatherThanDerivedFromIdentitySpelling()
    {
        string cardJson = File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));
        string setupJson = File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json"));
        var renamedCards = CardCatalog.Parse(cardJson.Replace("spider_man_nemesis", "declared_rival", StringComparison.Ordinal));
        var renamedSetup = SetupCatalog.Parse(setupJson.Replace("spider_man_nemesis", "declared_rival", StringComparison.Ordinal));
        var dealt = Dealer.DealOrder(renamedSetup, "rhino", ["spider_man"], facts: renamedCards);
        Assert.Contains(dealt, card => card.Source == CreationSource.Nemesis);
        Assert.Throws<ArgumentException>(() => Dealer.DealOrder(renamedSetup, "rhino", ["spider_man"],
            facts: CardCatalog.Parse(cardJson)));
    }

    [Theory]
    [InlineData("villain")]
    [InlineData("schemes")]
    public void IncompleteScenarioCannotBeAdvertised(string missing)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        JsonNode data = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")))!;
        data["campaigns"]!["rhino"]![missing] = new JsonArray();
        var setup = SetupCatalog.Parse(data.ToJsonString());
        var program = AbilityLowering.Book(AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));
        Assert.Throws<ArgumentException>(() => RuntimeProductAdmission.Discover(setup, cards, program.Authored));
    }

}
