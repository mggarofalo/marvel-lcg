using System.Net;
using Marvel.Protocol;
using Marvel.Tests;
using Marvel.Transport;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class DeckCatalogTransportTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicCatalogAndStructuredCorrectionsCrossBothTransports(bool socket)
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var request = EngineRequest.ReadDecks("catalog");
        var response = socket
            ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
        Assert.Null(response.Error);
        Assert.Null(response.World);
        Assert.Null(response.Capability);
        var catalog = response.Decks!.Catalog!;
        Assert.Equal(["captain_america", "spider_man", "captain_marvel", "she_hulk", "iron_man", "black_panther", "hawkeye", "spider_woman"],
            catalog.Heroes.Select(hero => hero.Key));
        var kick = Assert.Single(catalog.Cards, card => card.Face == "01005");
        Assert.Equal("3", kick.Cost);
        Assert.Null(kick.Preview!.TargetId);
        Assert.Contains("8 damage", kick.Preview.RulesText);
        Assert.Contains("8 damage", kick.Text);
        Assert.DoesNotContain(catalog.Cards, card => card.Face == "02001a");
        var hero = catalog.Heroes.Single(item => item.Key == "black_panther");
        request = EngineRequest.ReadDecks("assess", new(hero.Key, hero.Starter.Skip(1).ToArray()));
        response = socket
            ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
        Assert.Null(response.Error);
        Assert.Null(response.Decks!.Catalog);
        Assert.Equal(39, response.Decks.Assessment!.Size);
        Assert.Contains(response.Decks.Assessment.Diagnostics, item => item.Code == "signature-quantity");
        Assert.Contains(response.Decks.Assessment.Diagnostics, item => item.Code == "deck-size");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResearchPagesPreservePreviewsAndRemainBounded(bool socket)
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var request = EngineRequest.ReadDecks("research", new("", []) { CatalogPage = 1 });
        var response = socket
            ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
        Assert.Null(response.Error);
        Assert.Null(response.Capability);
        Assert.Null(response.World);
        var page = response.Decks!.Page!;
        Assert.Equal(200, page.Cards.Count);
        Assert.Equal(2, page.NextPage);
        Assert.All(page.Cards, card => Assert.Equal(card.Face, card.Preview!.FaceId));
        Assert.Contains(page.Cards, card => !card.RuntimeSupported);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PartialDeckPermissionCrossesBothTransportsWithoutEnablingPlay(bool socket)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var host = new EngineHost(factory);
        var hero = factory.DiscoverDecks().Heroes[0];
        var request = EngineRequest.ReadDecks("partial", new(hero.Key, hero.Signature));
        var response = socket
            ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
        Assert.True(response.Decks!.Assessment!.CanExtend);
        Assert.Contains(response.Decks.Assessment.Diagnostics, item => item.Code == "deck-size");
        Assert.False(factory.AssessDeck(hero.Key, hero.Signature.Skip(1).ToArray()).CanExtend);
        Assert.False(factory.AssessDeck(hero.Key, [.. hero.Signature, "unknown"]).CanExtend);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RowQuantitiesRespectPrintedLimitsAndExcludeRequiredCards(bool socket)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var host = new EngineHost(factory);
        var hero = factory.DiscoverDecks().Heroes.Single(item => item.Key == "spider_man");
        var request = EngineRequest.ReadDecks("rows", new(hero.Key, hero.Signature) { IncludeQuantities = true });
        var response = socket
            ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
        var quantities = response.Decks!.Assessment!.Quantities;
        Assert.Equal([0, 1], quantities["01084"]);
        Assert.Equal([0, 1], quantities["01088"]);
        Assert.Equal([0, 1, 2, 3], quantities["01087"]);
        Assert.All(hero.Signature, face => Assert.False(quantities.ContainsKey(face)));
        Assert.Equal([0], quantities["03001a"]);
        Assert.False(quantities.ContainsKey("05001a"));
    }

    [Fact]
    public void DeckRequestsRejectGameCapabilitiesOversizedDraftsAndWrongOperations()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var query = EngineRequest.ReadDecks("decks", new("spider_man", ["unknown"]));
        Assert.Equal("invalid_request", host.Exchange(query with { Capability = "not-a-capability" }).Error!.Code);
        Assert.Equal("invalid_request", host.Exchange(query with { Operation = EngineProtocol.Setup }).Error!.Code);
        Assert.Equal("invalid_request", host.Exchange(query with { Deck = new("spider_man", new string[501]) }).Error!.Code);
        Assert.Equal("invalid_request", host.Exchange(query with { Deck = new("unsupported-hero", []) }).Error!.Code);
    }
}
