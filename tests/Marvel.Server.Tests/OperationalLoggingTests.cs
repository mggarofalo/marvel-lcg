using System.Text.Json;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class OperationalLoggingTests
{
    [Fact]
    public void EngineOutcomesAreStructuredAndOmitSecretsAndGameplayPayloads()
    {
        var sink = new CollectingSink();
        var log = new OperationalLog(
            sink,
            "test-process",
            () => new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero));
        var host = new EngineHost(
            DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root),
            new SequenceCapabilities("owner-secret", "invitation-secret"),
            new RestrictedVisibilityPolicy(0),
            new MemorySessionStore(),
            log);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open-request",
            "logged-table",
            new GameSpecification(
                "rhino", ["captain_marvel", "spider_man"], [], Seed: 73)));
        var mulligan = Assert.IsType<Prompt>(opened.Prompt);
        EngineResponse resolved = host.Exchange(EngineRequest.ResolveGame(
            "resolve-request",
            "logged-table",
            Assert.IsType<string>(opened.Capability),
            new EngineDecision(Assert.Single(mulligan.Affordances).Id, []),
            opened.Revision));
        EngineResponse stale = host.Exchange(EngineRequest.ResolveGame(
            "owner-secret",
            "logged-table",
            Assert.IsType<string>(opened.Capability),
            new EngineDecision(Assert.Single(mulligan.Affordances).Id, []),
            opened.Revision));
        EngineResponse invalid = host.Exchange(new EngineRequest(
            EngineProtocol.Version,
            "owner-secret",
            "owner-secret",
            "owner-secret",
            "owner-secret"));

        Assert.Null(resolved.Error);
        Assert.Equal("stale_decision", stale.Error?.Code);
        Assert.Equal("invalid_request", invalid.Error?.Code);
        WaitForRecords(sink, 7);
        IReadOnlyList<OperationalRecord> requests = sink.Records
            .Where(record => record.EventId == OperationalEventIds.RequestCompleted)
            .ToList();
        Assert.Collection(
            requests,
            record =>
            {
                Assert.Equal(OperationalEventIds.RequestCompleted, record.EventId);
                Assert.Equal("open", record.Operation);
                Assert.Equal("accepted", record.Disposition);
                Assert.True(record.SaveCommitted);
                Assert.False(record.ReplayVerified);
                Assert.Null(record.AuthorizedSeat);
            },
            record =>
            {
                Assert.Equal("resolve", record.Operation);
                Assert.Equal(0, record.AuthorizedSeat);
                Assert.True(record.SaveCommitted);
                Assert.True(record.ReplayVerified);
                Assert.Equal(resolved.Revision, record.Revision);
            },
            record =>
            {
                Assert.Equal("stale", record.Disposition);
                Assert.Equal("stale_decision", record.ErrorCode);
                Assert.Equal(resolved.Revision, record.Revision);
                Assert.False(record.SaveCommitted);
                Assert.False(record.ReplayVerified);
            },
            record =>
            {
                Assert.Equal("unknown", record.Operation);
                Assert.Equal("rejected", record.Disposition);
                Assert.Equal("invalid_request", record.ErrorCode);
            });
        string serialized = JsonSerializer.Serialize(sink.Records);
        Assert.DoesNotContain("owner-secret", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("invitation-secret", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("capability", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("invitation", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payment", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exception", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FieldsAreBoundedAndTheJsonSinkUsesNamedMachineFields()
    {
        using var output = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        var log = new OperationalLog(
            new JsonTextOperationalSink(output),
            new string('p', 400),
            () => DateTimeOffset.UnixEpoch);

        log.Write(
            new string('e', 400),
            new string('d', 400),
            durationMilliseconds: -1,
            requestId: new string('r', 400),
            gameId: new string('g', 400),
            operation: new string('o', 400),
            errorCode: new string('x', 400));
        log.Flush(TimeSpan.FromSeconds(2));

        using JsonDocument document = JsonDocument.Parse(output.ToString());
        JsonElement root = document.RootElement;
        Assert.Equal(256, root.GetProperty("event_id").GetString()!.Length);
        Assert.Equal(256, root.GetProperty("process").GetString()!.Length);
        Assert.Equal(32, root.GetProperty("request_id").GetString()!.Length);
        Assert.Equal(32, root.GetProperty("game_id").GetString()!.Length);
        Assert.Equal("unknown", root.GetProperty("operation").GetString());
        Assert.Equal("unknown_error", root.GetProperty("error_code").GetString());
        Assert.Equal(0, root.GetProperty("duration_milliseconds").GetInt64());
        Assert.Equal(DateTimeOffset.UnixEpoch, root.GetProperty("timestamp_utc").GetDateTimeOffset());
        Assert.DoesNotContain(
            root.EnumerateObject().Select(property => property.Name),
            name => name.Contains("message", StringComparison.OrdinalIgnoreCase)
                || name.Contains("secret", StringComparison.OrdinalIgnoreCase)
                || name.Contains("card", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("game_aborted")]
    [InlineData("history_authority")]
    [InlineData("history_direction")]
    [InlineData("history_frontier")]
    [InlineData("history_open")]
    [InlineData("reorder_kind")]
    [InlineData("reorder_shape")]
    public void CanonicalHistoryErrorsRemainMachineReadable(string errorCode)
    {
        var sink = new CollectingSink();
        var log = new OperationalLog(sink, "test");

        log.Write(
            OperationalEventIds.RequestCompleted,
            "rejected",
            operation: EngineProtocol.Undo,
            errorCode: errorCode);

        WaitForRecords(sink, 1);
        Assert.Equal(errorCode, Assert.Single(sink.Records).ErrorCode);
    }

    [Fact]
    public void AFailingSinkCannotChangeAnEngineResponse()
    {
        GameSpecification game = new("rhino", ["spider_man"], [], Seed: 73);
        var observed = new EngineHost(
            DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root),
            new SequenceCapabilities("same-owner"),
            log: new OperationalLog(new ThrowingSink(), "test"));
        var silent = new EngineHost(
            DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root),
            new SequenceCapabilities("same-owner"));

        EngineResponse withFailure = observed.Exchange(
            EngineRequest.OpenGame("same-request", "same-game", game));
        EngineResponse withoutObserver = silent.Exchange(
            EngineRequest.OpenGame("same-request", "same-game", game));

        Assert.Equal(
            EngineJson.Write(withoutObserver),
            EngineJson.Write(withFailure));
    }

    [Fact]
    public async Task ABlockingSinkCannotDelayAnEngineResponse()
    {
        using var sink = new BlockingSink();
        var host = new EngineHost(
            DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root),
            new SequenceCapabilities("owner"),
            log: new OperationalLog(sink, "test"));

        Task<EngineResponse> exchange = Task.Run(() => host.Exchange(
            EngineRequest.OpenGame(
                "request", "game",
                new GameSpecification("rhino", ["spider_man"], [], Seed: 73))));

        try
        {
            EngineResponse response = await exchange.WaitAsync(
                TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            Assert.Null(response.Error);
            Assert.True(sink.Entered.Wait(
                TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        }
        finally
        {
            sink.Release.Set();
        }
    }

    [Fact]
    public async Task TransportFailureIsStructuredWithoutItsCapability()
    {
        int port;
        using (var unavailable = new TcpListener(IPAddress.Loopback, 0))
        {
            unavailable.Start();
            port = ((IPEndPoint)unavailable.LocalEndpoint).Port;
            unavailable.Stop();
        }

        var sink = new CollectingSink();
        var transport = new SocketTransport(
            IPAddress.Loopback.ToString(),
            port,
            new OperationalLog(sink, "Marvel.Godot"));

        await Assert.ThrowsAsync<EngineTransportException>(async () =>
            await transport.ExchangeAsync(
                EngineRequest.CloseGame(
                    "transport-request", "transport-game", "transport-capability"),
                TestContext.Current.CancellationToken));

        WaitForRecords(sink, 1);
        OperationalRecord record = Assert.Single(sink.Records);
        Assert.Equal(OperationalEventIds.TransportCompleted, record.EventId);
        Assert.Equal("rejected", record.Disposition);
        Assert.Equal("transport_failed", record.ErrorCode);
        Assert.Null(record.SaveCommitted);
        Assert.Null(record.ReplayVerified);
        Assert.DoesNotContain(
            "transport-capability",
            JsonSerializer.Serialize(record),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(EngineProtocol.Setup)]
    [InlineData(EngineProtocol.Close)]
    public async Task TransportOmitsRevisionWhenTheResponseHasNoAuthoritativeRevision(
        string operation)
    {
        EngineRequest request = operation == EngineProtocol.Setup
            ? EngineRequest.ReadSetup("request")
            : EngineRequest.CloseGame("request", "game", "owner");
        var endpoint = new ConstantEndpoint(new EngineResponse(
            EngineProtocol.Version,
            request.RequestId,
            request.GameId,
            Capability: null,
            Prompt: null,
            Events: []));
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
        var transport = new SocketTransport(
            IPAddress.Loopback.ToString(),
            port,
            new OperationalLog(sink, "Marvel.Godot"));

        try
        {
            EngineResponse response = await transport.ExchangeAsync(
                request, TestContext.Current.CancellationToken);
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
            SocketFrame.Write(stream, System.Text.Encoding.UTF8.GetBytes(
                $"{{\"version\":{EngineProtocol.Version},\"request_id\":\"request\",\"game_id\":\"\","
                + "\"capability\":null,\"prompt\":null,\"events\":[],"
                + "\"error\":{\"code\":null,\"message\":\"owner-secret\"}}"));
        }, TestContext.Current.CancellationToken);
        var sink = new CollectingSink();
        var transport = new SocketTransport(
            IPAddress.Loopback.ToString(),
            port,
            new OperationalLog(sink, "Marvel.Godot"));

        try
        {
            await Assert.ThrowsAsync<EngineTransportException>(async () =>
                await transport.ExchangeAsync(
                    EngineRequest.ReadSetup("request"),
                    TestContext.Current.CancellationToken));
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

    private sealed class CollectingSink : IOperationalSink
    {
        private readonly ConcurrentQueue<OperationalRecord> records = new();

        public int Count => records.Count;

        public IReadOnlyList<OperationalRecord> Records => [.. records];

        public void Write(OperationalRecord record) => records.Enqueue(record);
    }

    private static void WaitForRecords(CollectingSink sink, int count) =>
        Assert.True(SpinWait.SpinUntil(
            () => sink.Count >= count, TimeSpan.FromSeconds(2)));

    private sealed class ThrowingSink : IOperationalSink
    {
        public void Write(OperationalRecord record) =>
            throw new IOException("sink-private-detail");
    }

    private sealed class BlockingSink : IOperationalSink, IDisposable
    {
        public ManualResetEventSlim Entered { get; } = new(false);

        public ManualResetEventSlim Release { get; } = new(false);

        public void Write(OperationalRecord record)
        {
            Entered.Set();
            Release.Wait();
        }

        public void Dispose()
        {
            Release.Set();
            Entered.Dispose();
            Release.Dispose();
        }
    }

    private sealed class SequenceCapabilities(params string[] values)
        : ISessionCapabilityIssuer
    {
        private readonly Queue<string> values = new(values);

        public string Issue() => values.Dequeue();
    }

    private sealed class ConstantEndpoint(EngineResponse response) : IEngineEndpoint
    {
        public EngineResponse Exchange(EngineRequest request) => response;
    }
}
