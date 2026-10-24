using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>Canonical research metadata projection is independent of public product admission.</summary>
public sealed class WreckingSetupProjectionTests
{
    [Fact]
    public void CanonicalNineteenRecipesRetainTheirModeAndPhysicalGroupVersionsOnTheWire()
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var choices = setup.CampaignNames.Where(key => key.StartsWith("wrecking_crew_", StringComparison.Ordinal))
            .Select(key => ScenarioSetupProjection.Describe(key, setup.Campaign(key), cards)).ToArray();
        var admitted = DatasetGameFactory.Load(RepositoryPaths.Root).DiscoverSetup();
        Assert.DoesNotContain(admitted.Scenarios, choice => choice.Name == "Wrecking Crew");
        var response = new EngineResponse(EngineProtocol.Version, "canonical-research", "", null, null, [])
            { Setup = admitted with { Scenarios = choices } };
        var actual = EngineJson.ReadResponse(EngineJson.Write(response)).Setup!.Scenarios;
        Assert.Equal(19, actual.Count);
        Assert.Equal(19, actual.Select(choice => choice.Key).Distinct().Count());
        foreach (var choice in actual)
        {
            Assert.Equal("Wrecking Crew", choice.Name);
            Assert.False(choice.AllowsModularSets);
            Assert.Empty(choice.RecommendedModularSets);
            Assert.Equal(["Wrecker", "Thunderball", "Piledriver", "Bulldozer"], choice.Variants.Select(item => item.Name));
            Assert.Equal(ExpectedVersions(choice), choice.Variants.Select(item => item.Value));
            Assert.Equal(choice.Key.Split('_')[2], choice.Mode);
        }
    }

    private static IEnumerable<string> ExpectedVersions(ScenarioSetupChoice choice) => choice.Mode switch
    {
        "standard" => Enumerable.Repeat("A", 4),
        "expert" => Enumerable.Repeat("B", 4),
        "extreme" => Enumerable.Repeat("A → B", 4),
        "custom" => choice.Key.Split('_')[3].Select(value => value.ToString().ToUpperInvariant()),
        _ => throw new InvalidOperationException("unexpected canonical mode"),
    };
}
