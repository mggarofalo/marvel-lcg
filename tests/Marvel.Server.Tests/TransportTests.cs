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
            "correlation", "game", "capability", new EngineDecision(4, [11], [7]));
        var expected = new EngineResponse(
            EngineProtocol.Version,
            request.RequestId,
            request.GameId,
            Capability: null,
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
            "capability",
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
                EngineRequest.CloseGame("cancelled", "game", "capability"),
                cancelled.Token));
        Assert.Equal(0, endpoint.Calls);
    }

    [Fact]
    public async Task CallerCancellationStopsAtTheCompletedRequestWrite()
    {
        var request = EngineRequest.CloseGame("commit", "game", "capability");
        var response = new EngineResponse(
            EngineProtocol.Version,
            request.RequestId,
            request.GameId,
            Capability: null,
            Prompt: null,
            Events: [],
            Error: new EngineError("session_not_found", "missing"));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task serving = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            Assert.NotNull(SocketFrame.Read(stream));
            SocketFrame.Write(stream, EngineJson.Write(response));
        }, TestContext.Current.CancellationToken);
        using var cancelled = new CancellationTokenSource();
        var socket = new SocketTransport(
            IPAddress.Loopback.ToString(), port, cancelled.Cancel);

        Task<EngineResponse> exchange = socket.ExchangeAsync(request, cancelled.Token).AsTask();

        Assert.Equal(
            EngineJson.Write(response),
            EngineJson.Write(await exchange));
        await serving;
    }

    [Fact]
    public async Task LargeClientIdsAreBoundedAndTheNextConnectionStillWorks()
    {
        var server = new SocketEngineServer(
            new EngineHost(new UnusedFactory()), IPAddress.Loopback, port: 0);
        string largeGameId = new('g', 2_200_000);

        var rejected = await ExchangeOverSocket(
            server,
            EngineRequest.CloseGame("large", largeGameId, "capability"));
        var next = await ExchangeOverSocket(
            server,
            EngineRequest.CloseGame("next", "missing", "capability"));

        Assert.Equal("invalid_request", rejected.Error?.Code);
        Assert.Equal(EngineProtocol.MaximumIdentifierLength, rejected.GameId.Length);
        Assert.True(EngineJson.Write(rejected).Length < SocketFrame.MaximumPayload);
        Assert.Equal("session_not_found", next.Error?.Code);
    }

    [Fact]
    public async Task AnUnrepresentableResponseIsContainedToItsConnection()
    {
        var normal = new EngineResponse(
            EngineProtocol.Version, "next", "game", Capability: null,
            Prompt: null, Events: []);
        var endpoint = new SequenceEndpoint(
            new EngineResponse(
                EngineProtocol.Version,
                "large",
                new string('g', SocketFrame.MaximumPayload),
                Capability: null,
                Prompt: null,
                Events: []),
            normal);
        var server = new SocketEngineServer(endpoint, IPAddress.Loopback, port: 0);

        var failed = await ExchangeOverSocket(
            server, EngineRequest.CloseGame("first", "game", "capability"));
        var next = await ExchangeOverSocket(
            server, EngineRequest.CloseGame("second", "game", "capability"));

        Assert.Equal("response_failed", failed.Error?.Code);
        Assert.Equal(EngineJson.Write(normal), EngineJson.Write(next));
    }

    [Fact]
    public async Task SeparateConnectionsCannotResolveOrCloseEachOthersSession()
    {
        var host = new EngineHost(
            DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root),
            new SequenceCapabilities("capability-a", "capability-b"));
        var server = new SocketEngineServer(host, IPAddress.Loopback, port: 0);
        var specification = new GameSpecification(
            "rhino", ["spider_man"], ModularSets: [], Seed: 7);

        var first = await ExchangeOverSocket(
            server, EngineRequest.OpenGame("first", "same-id", specification));
        var second = await ExchangeOverSocket(
            server, EngineRequest.OpenGame("second", "same-id", specification));
        var guessed = await ExchangeOverSocket(
            server, EngineRequest.CloseGame("guess", "same-id", "not-the-capability"));
        var closeSecond = await ExchangeOverSocket(
            server,
            EngineRequest.CloseGame("close-second", "same-id", second.Capability!));
        var resolveFirst = await ExchangeOverSocket(
            server,
            EngineRequest.ResolveGame(
                "resolve-first", "same-id", first.Capability!, EngineDecision.Decline));

        Assert.NotEqual(first.Capability, second.Capability);
        Assert.Equal("session_not_found", guessed.Error?.Code);
        Assert.Null(closeSecond.Error);
        Assert.Null(resolveFirst.Error);
    }

    private static async Task<EngineResponse> ExchangeOverSocket(
        SocketEngineServer server, EngineRequest request)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task serving = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            server.Serve(accepted);
        }, TestContext.Current.CancellationToken);
        var transport = new SocketTransport(IPAddress.Loopback.ToString(), port);

        EngineResponse response = await transport.ExchangeAsync(
            request, TestContext.Current.CancellationToken);
        await serving;
        return response;
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

    private sealed class SequenceEndpoint(params EngineResponse[] responses)
        : IEngineEndpoint
    {
        private readonly Queue<EngineResponse> responses = new(responses);

        public EngineResponse Exchange(EngineRequest request) => responses.Dequeue();
    }

    private sealed class SequenceCapabilities(params string[] capabilities)
        : ISessionCapabilityIssuer
    {
        private readonly Queue<string> capabilities = new(capabilities);

        public string Issue() => capabilities.Dequeue();
    }

    private sealed class UnusedFactory : IGameFactory
    {
        public OpenedGame Create(GameSpecification specification) =>
            throw new InvalidOperationException("should not be called");
    }
}
