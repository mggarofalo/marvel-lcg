using Marvel.Protocol;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class RiseOfRedSkullDeckDiscoveryTests
{
    [Theory]
    [InlineData("hawkeye")]
    [InlineData("spider_woman")]
    public void DiscoveredStarterPassesTheSameAssessmentUsedToOpenGames(string key)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var hero = factory.DiscoverDecks().Heroes.Single(hero => hero.Key == key);
        Assert.Equal(40, hero.Starter.Count);
        Assert.True(factory.AssessDeck(key, hero.Starter).IsValid);
        Assert.NotNull(factory.Create(new GameSpecification("rhino", [key], [], 17,
            [new(hero.Starter)])));
        Assert.False(factory.AssessDeck(key, hero.Starter.Skip(1).ToArray()).CanExtend);
    }

    [Fact]
    public void SpiderWomanAspectBalanceIsCheckedThroughTheAdmittedCatalog()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var hero = factory.DiscoverDecks().Heroes.Single(hero => hero.Key == "spider_woman");
        // Double Agent: "Choose two aspects instead of one ... an equal number of cards from those aspects".
        string[] imbalanced = [.. hero.Starter, "01056"];
        var assessment = factory.AssessDeck(hero.Key, imbalanced);
        Assert.False(assessment.IsValid);
        Assert.Contains(assessment.Diagnostics, item => item.Code == "aspect-balance");
        Assert.True(factory.AssessDeck(hero.Key, [.. imbalanced, "01059"]).IsValid);
        Assert.False(factory.AssessDeck(hero.Key, [.. hero.Starter, "02001a"]).CanExtend);
    }
}
