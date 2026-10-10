using System.Net;
using Marvel.Rules.Events;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class BoostTransportTests : TransportTestBase
{
    [Fact]
    public async Task BothTransportsRetainTheResolvedContribution()
    {
        var request = EngineRequest.SyncGame("boost", "table", "capability");
        var boost = new BoostResolved(7, 9, 2, false, 5)
        {
            Subjects = new Dictionary<int, string> { [7] = "Assault", [9] = "Rhino" },
            Trigger = "WhenEnemySchemes", Verb = "Boost",
        };
        var response = new EngineResponse(EngineProtocol.Version, request.RequestId,
            request.GameId, Capability: null, Prompt: null, Events: [boost]);
        var endpoint = new EchoEndpoint(request, response);
        var local = await new InProcessTransport(endpoint).ExchangeAsync(request, TestContext.Current.CancellationToken);
        var socket = await ExchangeOverSocket(new SocketEngineServer(endpoint, IPAddress.Loopback, 0), request);
        Assert.Equal(EngineJson.Write(local), EngineJson.Write(socket));
        foreach (var restored in new[] { local, socket })
        {
            var value = Assert.IsType<BoostResolved>(Assert.Single(restored.Events));
            Assert.Equal(2, value.Icons);
            Assert.Equal(5, value.Strength);
            Assert.False(value.Attacking);
            Assert.Equal("Rhino", value.Subjects![9]);
        }
    }
}
