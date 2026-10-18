using Marvel.Client;
using Marvel.Protocol;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DeckResponseTests
{
    [Theory]
    [InlineData("error")]
    [InlineData("capability")]
    [InlineData("correlation")]
    [InlineData("diagnostic")]
    [InlineData("quantity-range")]
    [InlineData("quantity-null")]
    [InlineData("quantity-duplicate")]
    public async Task MalformedReadOnlyResponsesCannotSupplyAnAssessment(string fault)
    {
        using var client = new LocalGameClient(new FaultyTransport(fault));
        var result = await client.ReadDecksAsync(new("spider_man", []), TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Null(result.Decks);
        Assert.NotNull(result.Error);
    }

    private sealed class FaultyTransport(string fault) : IEngineTransport
    {
        public ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default)
        {
            var response = new EngineResponse(EngineProtocol.Version, request.RequestId, "", null, null, [])
            {
                Decks = new(null, new(40, [])),
            };
            response = fault switch
            {
                "error" => response with { Error = new(null!, null!) },
                "capability" => response with { Capability = "unexpected-private-authority" },
                "correlation" => response with { RequestId = "unrelated" },
                "quantity-range" => response with { Decks = new(null, new(40, []) { Quantities = new Dictionary<string, int[]> { ["01087"] = [0, 4] } }) },
                "quantity-null" => response with { Decks = new(null, new(40, []) { Quantities = null! }) },
                "quantity-duplicate" => response with { Decks = new(null, new(40, []) { Quantities = new Dictionary<string, int[]> { ["01087"] = [0, 1, 1] } }) },
                "diagnostic" => response with { Decks = new(null, new(40, [null!])) },
                _ => throw new InvalidOperationException("Unknown response fault."),
            };
            return ValueTask.FromResult(response);
        }
    }
}
