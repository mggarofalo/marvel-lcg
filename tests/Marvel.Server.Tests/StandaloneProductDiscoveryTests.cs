using Marvel.Protocol;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class StandaloneProductDiscoveryTests
{
    [Fact]
    public void CandidateDiscoversExactlyTheCompleteProductsAndCreatesTheirGames()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var discovery = factory.DiscoverSetup();
        string[] heroes = ["black_panther", "captain_america", "captain_marvel", "hawkeye", "iron_man", "ms_marvel", "she_hulk", "spider_man", "spider_woman"];
        Assert.Equal(heroes,
            discovery.Heroes.Select(hero => hero.Key).Order(StringComparer.Ordinal));
        string[] scenarios = ["rhino", "klaw", "ultron", "crossbones", "absorbing_man", "taskmaster", "zola", "red_skull",
            "risky_business", "mutagen_formula"];
        string[] wrecking = ["wrecking_crew_standard", "wrecking_crew_expert", "wrecking_crew_extreme",
            .. Enumerable.Range(0, 16).Select(value => "wrecking_crew_custom_" + Convert.ToString(value, 2).PadLeft(4, '0').Replace('0', 'a').Replace('1', 'b'))];
        Assert.Equal(scenarios.SelectMany(key => new[] { key, key + "_expert" }).Concat(wrecking).Order(StringComparer.Ordinal),
            discovery.Scenarios.Select(scenario => scenario.Key).Order(StringComparer.Ordinal));
        Assert.NotNull(factory.CampaignDefinition);
        Assert.Contains(factory.DiscoverDecks().Heroes, hero => hero.Key == "captain_america");
        Assert.Contains(factory.DiscoverDecks().Cards, card => card.Face == "03024");
        Assert.Contains(factory.DiscoverDecks().Heroes, hero => hero.Key == "ms_marvel");
        Assert.Contains(factory.DiscoverDecks().Cards, card => card.Face == "05033");
        foreach (string scenario in wrecking)
            Assert.NotNull(factory.Create(new GameSpecification(scenario, ["captain_america"], [], 7)));
        foreach (string scenario in new[] { "risky_business", "risky_business_expert", "mutagen_formula", "mutagen_formula_expert" })
            Assert.NotNull(factory.Create(new GameSpecification(scenario, ["captain_america"], null, 7)));
        Assert.NotNull(factory.Create(new GameSpecification("rhino", ["captain_america"], [], 7)));
        Assert.NotNull(factory.Create(new GameSpecification("crossbones", ["hawkeye"], [], 7)));
        Assert.NotNull(factory.Create(new GameSpecification("rhino", ["ms_marvel"], [], 7)));
        Assert.NotNull(factory.Create(new GameSpecification("crossbones", ["ms_marvel"], [], 7)));
    }
}
