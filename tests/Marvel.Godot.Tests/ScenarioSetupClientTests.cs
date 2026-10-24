using Marvel.Protocol;
using Marvel.Server;
using Marvel.Tests;
using Marvel.Transport;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class ScenarioSetupClientTests : LocalGameClientTestBase
{
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public async Task LargerTablesOpenThroughTheActualHostWithEverySeatVisible(int seats)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var choices = factory.DiscoverSetup();
        using var client = new LocalGameClient(new InProcessTransport(new EngineHost(factory)));
        var selection = DefaultSelection(choices) with { HeroKeys = choices.Heroes.Take(seats).Select(hero => hero.Key).ToArray() };
        var opened = await client.OpenSessionAsync("larger-table", choices, selection, TestContext.Current.CancellationToken);
        Assert.True(opened.Succeeded, opened.Error?.Message);
        Assert.Equal(seats, opened.Response!.World!.Players.Count);
        Assert.Equal(seats, opened.Response.World.PlayerSummaries.Count);
        Assert.Empty(opened.Invitations);
        Assert.Equal(0, opened.Response.Prompt!.Player);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("mode")]
    [InlineData("duplicate")]
    [InlineData("missing-combination")]
    [InlineData("null-variants")]
    [InlineData("forbidden-modular")]
    [InlineData("inconsistent-modular")]
    [InlineData("inconsistent-recommendations")]
    public async Task ScenarioControlsRequireCompleteAndConsistentMetadata(string corruption)
    {
        var choices = Choices();
        var variants = new List<ScenarioSetupChoice>();
        for (int bits = 0; bits < 4; bits++) variants.Add(new("custom-" + bits, "Synthetic components", false, [])
        { Mode = "custom", Variants = [new("First", (bits & 1) == 0 ? "A" : "B"), new("Second", (bits & 2) == 0 ? "A" : "B")] });
        Corrupt(variants, choices, corruption);
        var response = new EngineResponse(EngineProtocol.Version, "local-setup", "", null, null, [],
            Setup: choices with { Scenarios = variants });
        var result = await new LocalGameClient(new FixedTransport(response)).ReadSetupAsync(TestContext.Current.CancellationToken);
        if (corruption == "none")
        {
            Assert.True(result.Succeeded);
            Assert.Equal(variants, result.Choices!.Scenarios);
        }
        else Assert.Equal("invalid_response", result.Error!.Code);
    }

    private static void Corrupt(List<ScenarioSetupChoice> variants, SetupChoices choices, string corruption)
    {
        switch (corruption)
        {
            case "mode": variants[0] = variants[0] with { Mode = "unknown" }; break;
            case "duplicate": variants[3] = variants[0]; break;
            case "missing-combination": variants.RemoveAt(3); break;
            case "null-variants": variants[0] = variants[0] with { Variants = null! }; break;
            case "inconsistent-modular": variants[0] = variants[0] with { AllowsModularSets = false }; break;
            case "inconsistent-recommendations": variants[0] = variants[0] with
                { RecommendedModularSets = [choices.ModularSets[0].Key] }; break;
            case "forbidden-modular": variants[0] = variants[0] with
                { AllowsModularSets = false, RecommendedModularSets = [choices.ModularSets[0].Key] }; break;
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task EverySeatRetainsItsSelectedHeroAndDeckInOneOpenRequest(int seats)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var choices = factory.DiscoverSetup();
        var heroes = factory.DiscoverDecks().Heroes.Take(seats).ToArray();
        var selection = new GameSetupSelection(heroes.Select(hero => hero.Key).ToArray(), choices.Scenarios[0].Key,
            ModularConfiguration.None, [], "73")
        { Decks = heroes.Select(hero => new DeckDraft(hero.Name, hero.IdentityFaces[0], hero.Starter)).ToArray() };
        var transport = new CapturingTransport();
        var result = await new LocalGameClient(transport).OpenSessionAsync("table", choices, selection,
            TestContext.Current.CancellationToken);
        Assert.Equal("stopped", result.Error!.Code);
        var request = Assert.Single(transport.Requests);
        Assert.Equal(selection.HeroKeys, request.Game!.Heroes);
        Assert.Equal(seats, request.Game.PlayerDecks!.Count);
        for (int seat = 0; seat < seats; seat++) Assert.Equal(heroes[seat].Starter, request.Game.PlayerDecks[seat].Cards);
    }

    [Fact]
    public async Task IndependentEncounterRecipesRejectModularSelectionsBeforeSending()
    {
        var choices = Choices();
        var original = choices.Scenarios.Single(choice => choice.Key == "rhino");
        choices = choices with { Scenarios = [original with { AllowsModularSets = false, RecommendedModularSets = [] }] };
        var transport = new CapturingTransport();
        var result = await new LocalGameClient(transport).OpenSessionAsync("table", choices,
            DefaultSelection(choices) with { Modular = ModularConfiguration.Selected, ModularKeys = [choices.ModularSets[0].Key] },
            TestContext.Current.CancellationToken);
        Assert.Equal("invalid_selection", result.Error!.Code);
        Assert.Empty(transport.Requests);
    }
}
