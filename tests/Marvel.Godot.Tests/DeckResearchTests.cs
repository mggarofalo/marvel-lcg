using Marvel.Client;
using Marvel.Server;
using Marvel.Tests;
using Marvel.Transport;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DeckResearchTests
{
    [Fact]
    public async Task ResearchCatalogIncludesExpansionsWithoutAdmittingThemToGameplay()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        using var client = new LocalGameClient(new InProcessTransport(new EngineHost(factory)));
        var admitted = factory.DiscoverDecks();
        var research = await DeckResearchCatalog.LoadAsync(client, admitted, TestContext.Current.CancellationToken);
        Assert.True(research.Cards.Count > 4000);
        Assert.Equal(research.Cards.Count, research.Cards.Select(card => card.Face).Distinct().Count());
        Assert.Equal(admitted.Heroes, research.Heroes);
        var captain = Assert.Single(research.Cards, card => card.Face == "03001a");
        Assert.False(captain.RuntimeSupported);
        Assert.NotNull(captain.Preview);
        Assert.Contains("Captain America", captain.Preview.Title);
        var hero = admitted.Heroes[0];
        Assert.False(factory.AssessDeck(hero.Key, hero.Starter.Append(captain.Face).ToArray()).IsValid);
        Assert.Single(CardSearch.Find(research.Cards, "03001a"));
        Assert.NotEmpty(CardSearch.Find(research.Cards, "e>core t:hero"));
    }

    [Fact]
    public async Task ResearchRequestsRejectDraftAuthorityAndUnboundedPages()
    {
        using var client = new LocalGameClient(new InProcessTransport(new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root))));
        var result = await client.ReadDecksAsync(new("spider_man", []) { CatalogPage = 0 }, TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        result = await client.ReadDecksAsync(new("", []) { CatalogPage = 1001 }, TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
    }
}
