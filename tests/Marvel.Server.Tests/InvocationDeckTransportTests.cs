using System.Net;
using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Transport;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class InvocationDeckTransportTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TypedPilesCrossBothTransportsAndOwnTheirStorage(bool socket)
    {
        // Synthetic public contract: area purpose does not depend on readable card faces.
        var request = EngineRequest.SyncGame("invocations", "table", "capability");
        var expected = Response(request.RequestId);
        var endpoint = new EchoEndpoint(request, expected);
        var received = socket
            ? await ExchangeOverSocket(new SocketEngineServer(endpoint, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(endpoint).ExchangeAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(44, received.Version);
        Assert.Equal(EngineJson.Write(expected), EngineJson.Write(received));
        var piles = Assert.Single(received.World!.InvocationDecks);
        Assert.Equal(new InvocationDeckDescriptor(1, 4, 5), piles);
        Assert.Equal("INVOCATION DECK", Assert.Single(BoardPresentation.From(received.World).Areas,
            area => area.Id == piles.Deck).Title);
        Assert.IsType<InvocationDeckDescriptor[]>(expected.World!.InvocationDecks)[0] = piles with { Deck = 99 };
        Assert.Equal(4, received.World.InvocationDecks[0].Deck);
        Assert.IsAssignableFrom<IList<InvocationDeckDescriptor>>(received.World.InvocationDecks)[0] = piles with { Deck = 77 };
        Assert.Equal(99, expected.World.InvocationDecks[0].Deck);
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("missing")]
    [InlineData("swapped")]
    [InlineData("shared")]
    [InlineData("duplicate")]
    [InlineData("null")]
    public void InvalidRelationshipsAreRejectedBeforePublication(string corruption)
    {
        var response = Response("invalid");
        var deck = response.World!.InvocationDecks[0];
        IReadOnlyList<InvocationDeckDescriptor> invalid = corruption switch
        {
            "owner" => [deck with { Owner = 0 }],
            "missing" => [deck with { Deck = 99 }],
            "swapped" => [deck with { Deck = 5, Discard = 4 }],
            "shared" => [deck with { Discard = 4 }],
            "duplicate" => [deck, deck],
            "null" => null!,
            _ => throw new ArgumentException(corruption),
        };
        var broken = response with { World = response.World with { InvocationDecks = invalid } };
        Assert.Throws<JsonException>(() => EngineJson.ReadResponse(EngineJson.Write(broken)));
        Assert.Equal(deck, Assert.Single(EngineJson.Snapshot(response).World!.InvocationDecks));
    }

    [Theory]
    [InlineData("wrong-owner")]
    [InlineData("duplicate-id")]
    [InlineData("hosted")]
    public void PileBindingCannotAliasAnotherSeatOrArea(string corruption)
    {
        var response = Response("binding");
        var areas = response.World!.Areas.ToArray();
        areas = corruption switch
        {
            "wrong-owner" => [areas[0] with { Owner = 0 }, areas[1]],
            "duplicate-id" => [.. areas, areas[0] with { Zone = "Deck" }],
            "hosted" => [areas[0] with { Host = 12 }, areas[1]],
            _ => throw new ArgumentException(corruption),
        };
        var broken = response with { World = response.World with { Areas = areas } };
        Assert.Throws<JsonException>(() => EngineJson.ReadResponse(EngineJson.Write(broken)));
        Assert.True(InvocationDeckContract.IsValid(response.World));
    }

    private static EngineResponse Response(string request) =>
        new(EngineProtocol.Version, request, "table", null, null, [],
            new WorldDescriptor([new PlayerDescriptor(1, "Owner", false)],
                [new AreaDescriptor(4, "AdditionalDeck", 1, -1, [], []),
                 new AreaDescriptor(5, "AdditionalDiscardPile", 1, -1, [], [])], [], Outcome.Unfinished)
            { InvocationDecks = new[] { new InvocationDeckDescriptor(1, 4, 5) } });
}
