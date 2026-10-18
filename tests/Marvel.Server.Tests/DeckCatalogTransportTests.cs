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
        Assert.Equal(5, catalog.Heroes.Count);
        var kick = Assert.Single(catalog.Cards, card => card.Face == "01005");
        Assert.Equal("3", kick.Cost);
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
