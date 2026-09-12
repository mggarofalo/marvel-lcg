using System.Text.Json;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;
public sealed class OperationalLoggingTransportOmitsRevisionTests : OperationalLoggingTestBase
{
    [Theory]
    [InlineData(EngineProtocol.Setup)]
    [InlineData(EngineProtocol.Close)]
    public async Task TransportOmitsRevisionWhenTheResponseHasNoAuthoritativeRevision(string operation)
    {
        EngineRequest request = operation == EngineProtocol.Setup ? EngineRequest.ReadSetup("request") : EngineRequest.CloseGame("request", "game", "owner");
        var endpoint = new ConstantEndpoint(new EngineResponse(EngineProtocol.Version, request.RequestId, request.GameId, Capability: null, Prompt: null, Events: []));
        var server = new SocketEngineServer(endpoint, IPAddress.Loopback, port: 0);
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task serving = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            server.Serve(accepted);
        }, TestContext.Current.CancellationToken);
        var sink = new CollectingSink();
        var transport = new SocketTransport(IPAddress.Loopback.ToString(), port, new OperationalLog(sink, "Marvel.Godot"));
        try
        {
            EngineResponse response = await transport.ExchangeAsync(request, TestContext.Current.CancellationToken);
            Assert.Null(response.Error);
        }
        finally
        {
            await serving;
        }

        WaitForRecords(sink, 1);
        Assert.Null(Assert.Single(sink.Records).Revision);
    }

    [Fact]
    public async Task TransportRejectsAMalformedPeerErrorWithoutLoggingItsValue()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task serving = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            Assert.NotNull(SocketFrame.Read(stream));
            SocketFrame.Write(stream, System.Text.Encoding.UTF8.GetBytes($"{{\"version\":{EngineProtocol.Version},\"request_id\":\"request\",\"game_id\":\"\"," + "\"capability\":null,\"prompt\":null,\"events\":[]," + "\"error\":{\"code\":null,\"message\":\"owner-secret\"}}"));
        }, TestContext.Current.CancellationToken);
        var sink = new CollectingSink();
        var transport = new SocketTransport(IPAddress.Loopback.ToString(), port, new OperationalLog(sink, "Marvel.Godot"));
        try
        {
            await Assert.ThrowsAsync<EngineTransportException>(async () => await transport.ExchangeAsync(EngineRequest.ReadSetup("request"), TestContext.Current.CancellationToken));
        }
        finally
        {
            await serving;
        }

        WaitForRecords(sink, 1);
        string record = JsonSerializer.Serialize(Assert.Single(sink.Records));
        Assert.DoesNotContain("owner-secret", record, StringComparison.Ordinal);
        Assert.Contains("transport_failed", record, StringComparison.Ordinal);
    }
}
