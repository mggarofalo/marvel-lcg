using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class TransportTests
{
    [Fact]
    public async Task SocketAndInProcessTransportsExposeTheSameContract()
    {
        var request = EngineRequest.ResolveGame(
            "correlation", "game", new EngineDecision(4, [11], [7]));
        var expected = new EngineResponse(
            EngineProtocol.Version,
            request.RequestId,
            request.GameId,
            new Prompt(
                0, Question.TurnOption, TimingPriority.Untimed, "WhenPlayerInTurn",
                "Choose", Cancellable: true, []),
            [new FieldSet(11, "damage", 0, 1) { Trigger = "WhenPlayerInTurn", Verb = "Attack" }]);
        var endpoint = new EchoEndpoint(request, expected);
        var inProcess = new InProcessTransport(endpoint);
        Assert.NotEmpty(EngineJson.Write(expected));

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = new SocketEngineServer(endpoint, IPAddress.Loopback, port: 0);
        Task serving = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            server.Serve(accepted);
        }, TestContext.Current.CancellationToken);

        var socket = new SocketTransport(IPAddress.Loopback.ToString(), port);
        EngineResponse remote;
        try
        {
            remote = await socket.ExchangeAsync(
                request, TestContext.Current.CancellationToken);
        }
        finally
        {
            await serving;
        }

        Assert.Equal(
            EngineJson.Write(await inProcess.ExchangeAsync(
                request, TestContext.Current.CancellationToken)),
            EngineJson.Write(remote));
        Assert.Equal(2, endpoint.Calls);
    }

    [Fact]
    public void DecisionsHaveFiveWireFieldsAndNoConvenienceGetters()
    {
        var request = EngineRequest.ResolveGame(
            "wire",
            "game",
            new EngineDecision(
                4,
                [11],
                [7],
                new Dictionary<string, long>(StringComparer.Ordinal) { ["X"] = 2 },
                [new ResourceAllocation(7, 0, "M")]));

        using JsonDocument document = JsonDocument.Parse(EngineJson.Write(request));
        JsonElement decision = document.RootElement.GetProperty("decision");

        Assert.Equal(
            ["affordance", "targets", "resources", "values", "allocations"],
            decision.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public void UnknownWireFieldsAreRejected()
    {
        byte[] request = Encoding.UTF8.GetBytes(
            """
            {"version":1,"request_id":"r","operation":"resolve","game_id":"g","decision":{"affordance":-1,"targets":[]},"surprise":true}
            """);

        Assert.Throws<JsonException>(() => EngineJson.ReadRequest(request));
    }

    [Fact]
    public void FramingPinsTheLengthAndRejectsOversizedPayloads()
    {
        using var frame = new MemoryStream();
        SocketFrame.Write(frame, [1, 2, 3]);

        Assert.Equal([0, 0, 0, 3, 1, 2, 3], frame.ToArray());
        Assert.Throws<InvalidDataException>(
            () => SocketFrame.Write(
                Stream.Null, new byte[SocketFrame.MaximumPayload + 1]));
    }

    [Fact]
    public void ATruncatedFrameIsNeverTreatedAsARequest()
    {
        using var frame = new MemoryStream([0, 0, 0, 2, 1]);

        Assert.Throws<EndOfStreamException>(() => SocketFrame.Read(frame));
    }

    [Fact]
    public async Task ACancelledInProcessExchangeNeverReachesGameState()
    {
        var endpoint = new CountingEndpoint();
        var transport = new InProcessTransport(endpoint);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await transport.ExchangeAsync(
                EngineRequest.CloseGame("cancelled", "game"), cancelled.Token));
        Assert.Equal(0, endpoint.Calls);
    }

    private sealed class EchoEndpoint(
        EngineRequest expectedRequest,
        EngineResponse response) : IEngineEndpoint
    {
        public int Calls { get; private set; }

        public EngineResponse Exchange(EngineRequest request)
        {
            Calls++;
            Assert.Equal(EngineJson.Write(expectedRequest), EngineJson.Write(request));
            return response;
        }
    }

    private sealed class CountingEndpoint : IEngineEndpoint
    {
        public int Calls { get; private set; }

        public EngineResponse Exchange(EngineRequest request)
        {
            Calls++;
            throw new InvalidOperationException("should not be called");
        }
    }
}
