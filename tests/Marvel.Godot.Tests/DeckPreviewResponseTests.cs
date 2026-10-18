using Marvel.Client;
using Marvel.Protocol;
using Marvel.Server;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DeckPreviewResponseTests
{
    [Theory]
    [InlineData("live-target")]
    [InlineData("missing-preview")]
    [InlineData("missing-traits")]
    [InlineData("wrong-face")]
    [InlineData("missing-metadata")]
    public async Task MalformedCatalogPreviewsAreRejectedBeforeRendering(string fault)
    {
        var catalog = DatasetGameFactory.Load(RepositoryPaths.Root).DiscoverDecks();
        var card = catalog.Cards[0];
        card = fault switch
        {
            "live-target" => card with { Preview = card.Preview! with { TargetId = 42 } },
            "missing-preview" => card with { Preview = null },
            "missing-traits" => card with { Preview = card.Preview! with { Traits = null! } },
            "wrong-face" => card with { Preview = card.Preview! with { FaceId = "unrelated" } },
            "missing-metadata" => card with { SearchFields = null! },
            _ => throw new ArgumentException("Unknown fault", nameof(fault)),
        };
        using var client = new LocalGameClient(new CatalogTransport(catalog with { Cards = [card, .. catalog.Cards.Skip(1)] }));
        var result = await client.ReadDecksAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Equal("invalid_response", result.Error!.Code);
    }

    private sealed class CatalogTransport(DeckCatalog catalog) : IEngineTransport
    {
        public ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new EngineResponse(EngineProtocol.Version, request.RequestId, "", null, null, []) { Decks = new(catalog, null) });
    }
}
