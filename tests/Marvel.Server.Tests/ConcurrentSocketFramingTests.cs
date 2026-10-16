using System.Net;
using System.Net.Sockets;
using Marvel.Protocol;
using Marvel.Transport;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class ConcurrentSocketFramingTests
{
    [Fact]
    public async Task AnIncompletePeerDoesNotHoldTheAcceptLoopAndCancellationDrainsIt()
    {
        using var stopping = new CancellationTokenSource();
        var listening = new TaskCompletionSource<IPEndPoint>(TaskCreationOptions.RunContinuationsAsynchronously);
        var server = new SocketEngineServer(new SetupEndpoint(), IPAddress.Loopback, 0);
        Task serving = server.RunAsync(address => listening.SetResult(address), stopping.Token);
        IPEndPoint address = await listening.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        using var stalled = new TcpClient();
        await stalled.ConnectAsync(address.Address, address.Port, TestContext.Current.CancellationToken);
        await stalled.GetStream().WriteAsync(new byte[] { 0, 0 }, TestContext.Current.CancellationToken);
        try
        {
            var transport = new SocketTransport(address.Address.ToString(), address.Port);
            EngineResponse response = await transport.ExchangeAsync(EngineRequest.ReadSetup("healthy"), TestContext.Current.CancellationToken)
                .AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal("healthy", response.RequestId);
            Assert.Equal("test", response.Error!.Code);
        }
        finally
        {
            await stopping.CancelAsync();
            await serving.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
        Assert.Equal(0, await stalled.GetStream().ReadAsync(new byte[1], TestContext.Current.CancellationToken).AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InvalidDeckValuesRejectOnlyTheirFrameAndKeepTheListenerAvailable()
    {
        using var stopping = new CancellationTokenSource();
        var listening = new TaskCompletionSource<IPEndPoint>(TaskCreationOptions.RunContinuationsAsynchronously);
        var server = new SocketEngineServer(new SetupEndpoint(), IPAddress.Loopback, 0);
        Task serving = server.RunAsync(address => listening.SetResult(address), stopping.Token);
        IPEndPoint address = await listening.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        try
        {
            using var malformed = new TcpClient();
            await malformed.ConnectAsync(address.Address, address.Port, TestContext.Current.CancellationToken);
            var request = System.Text.Json.Nodes.JsonNode.Parse(EngineJson.Write(EngineRequest.OpenGame(
                "invalid", "table", new GameSpecification("rhino", ["spider_man"], [], 91))))!;
            request["game"]!["player_decks"] = System.Text.Json.Nodes.JsonNode.Parse("[{\"cards\":null}]");
            NetworkStream stream = malformed.GetStream();
            await SocketFrame.WriteAsync(stream, System.Text.Encoding.UTF8.GetBytes(request.ToJsonString()), stopping.Token);
            byte[] rejected = (await SocketFrame.ReadAsync(stream, stopping.Token))!;
            Assert.Equal("invalid_frame", EngineJson.ReadResponse(rejected).Error!.Code);
            var transport = new SocketTransport(address.Address.ToString(), address.Port);
            EngineResponse response = await transport.ExchangeAsync(EngineRequest.ReadSetup("healthy"), stopping.Token)
                .AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal("healthy", response.RequestId);
        }
        finally
        {
            await stopping.CancelAsync();
            await serving.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
    }

    private sealed class SetupEndpoint : IEngineEndpoint
    {
        public EngineResponse Exchange(EngineRequest request) => new(EngineProtocol.Version,
            request.RequestId, request.GameId, null, null, [], Error: new EngineError("test", "complete frame admitted"));
    }
}
