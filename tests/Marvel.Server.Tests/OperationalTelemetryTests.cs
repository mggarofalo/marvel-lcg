using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class OperationalTelemetryTests
{
    [Fact]
    public void StructuredSignalsProduceBoundedMetricsAndCorrelatedSpans()
    {
        var exporter = new CollectingExporter();
        var sink = new OperationalTelemetrySink(exporter);

        sink.Write(Record(
            EngineProtocol.Open, "accepted", saveCommitted: true,
            requestId: "request-correlation"));
        sink.Write(Record(
            "persistence", "accepted",
            eventId: OperationalEventIds.PersistenceCompleted,
            saveCommitted: true));
        sink.Write(Record(EngineProtocol.Sync, "accepted"));
        sink.Write(Record(
            "reconnect", "accepted",
            eventId: OperationalEventIds.ReconnectCompleted));
        sink.Write(Record(
            EngineProtocol.Undo, "rejected", errorCode: "history_frontier"));
        sink.Write(Record(
            EngineProtocol.Reorder, "accepted", replayVerified: true));
        sink.Write(Record(
            operation: null,
            disposition: "rejected",
            eventId: OperationalEventIds.SessionRestoreFailed,
            errorCode: "replay_diverged"));
        sink.Write(Record(EngineProtocol.Close, "accepted"));
        OperationalTelemetrySink.Flush(TimeSpan.FromSeconds(2));

        Assert.Contains(exporter.Envelopes.SelectMany(value => value.Metrics),
            metric => metric.Name == "marvel.request.outcomes");
        Assert.Contains(exporter.Envelopes.SelectMany(value => value.Metrics),
            metric => metric.Name == "marvel.request.latency_ms" && metric.Value == 17);
        Assert.Contains(exporter.Envelopes.SelectMany(value => value.Metrics),
            metric => metric.Name == "marvel.sessions.reconnects");
        Assert.Contains(exporter.Envelopes.SelectMany(value => value.Metrics),
            metric => metric.Name == "marvel.saves.committed");
        Assert.Contains(exporter.Envelopes.SelectMany(value => value.Metrics),
            metric => metric.Name == "marvel.replay.divergences");
        TelemetryMetric refusal = Assert.Single(
            exporter.Envelopes.SelectMany(value => value.Metrics),
            metric => metric.Name == "marvel.undo.refusals");
        Assert.Equal("history_frontier", refusal.Dimensions["error_code"]);
        Assert.Contains(exporter.Envelopes.SelectMany(value => value.Metrics),
            metric => metric.Name == "marvel.trace_rewrites.accepted");
        Assert.Equal(
            [1, 0],
            exporter.Envelopes.SelectMany(value => value.Metrics)
                .Where(metric => metric.Name == "marvel.sessions.active")
                .Select(metric => metric.Value));

        TelemetryEnvelope opened = exporter.Envelopes[0];
        Assert.Equal(32, opened.Spans[0].TraceId.Length);
        string json = JsonSerializer.Serialize(exporter.Envelopes);
        Assert.DoesNotContain("request-correlation", json, StringComparison.Ordinal);
        Assert.DoesNotContain("game", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("card", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("capability", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NoOpExporterIsTheDefaultSafeConsumer()
    {
        var sink = new OperationalTelemetrySink(new NoOpTelemetryExporter());

        sink.Write(Record(EngineProtocol.Resolve, "accepted"));
    }

    [Fact]
    public void TransportAndHostSignalsShareATraceWithoutExposingTheirRequestLabel()
    {
        var exporter = new CollectingExporter();
        var sink = new OperationalTelemetrySink(exporter);
        OperationalRecord host = Record(
            EngineProtocol.Resolve, "accepted", requestId: "shared-request");
        OperationalRecord transport = host with
        {
            EventId = OperationalEventIds.TransportCompleted,
            Process = "Marvel.Godot",
        };
        OperationalRecord replay = host with
        {
            EventId = OperationalEventIds.ReplayCompleted,
            Operation = "replay",
        };
        OperationalRecord persistence = host with
        {
            EventId = OperationalEventIds.PersistenceCompleted,
            Operation = "persistence",
        };

        sink.Write(transport);
        sink.Write(replay);
        sink.Write(persistence);
        sink.Write(host);
        OperationalTelemetrySink.Flush(TimeSpan.FromSeconds(2));

        Assert.Equal(
            exporter.Envelopes[0].Spans[0].TraceId,
            exporter.Envelopes[3].Spans[0].TraceId);
        Assert.Equal(
            [OperationalEventIds.TransportCompleted,
             OperationalEventIds.ReplayCompleted,
             OperationalEventIds.PersistenceCompleted,
             OperationalEventIds.RequestCompleted],
            exporter.Envelopes.Select(value => value.Spans[0].Name));
        Assert.DoesNotContain(
            "shared-request",
            JsonSerializer.Serialize(exporter.Envelopes),
            StringComparison.Ordinal);
    }

    [Fact]
    public void HttpExporterMakesOneAttemptAndCarriesNoRawOperationalIds()
    {
        var handler = new RecordingHandler(HttpStatusCode.ServiceUnavailable);
        using var exporter = new HttpTelemetryExporter(
            new Uri("https://telemetry.example.test/v1"), handler);
        var envelope = new TelemetryEnvelope(
            1,
            [],
            [new TelemetrySpan(
                "event", new string('a', 32), DateTimeOffset.UnixEpoch, 1,
                new Dictionary<string, string>())]);

        Assert.Throws<HttpRequestException>(() => exporter.Export(envelope));
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public void CompositeSinkFailureDoesNotSuppressOtherObservers()
    {
        var collected = new CollectingSink();
        var composite = new CompositeOperationalSink(new ThrowingSink(), collected);
        OperationalRecord record = Record(EngineProtocol.Resolve, "accepted");

        composite.Write(record);

        Assert.True(SpinWait.SpinUntil(
            () => collected.Records.Count == 1, TimeSpan.FromSeconds(2)));
        Assert.Same(record, Assert.Single(collected.Records));
    }

    [Fact]
    public void BlockedObserverDoesNotSuppressIndependentLocalEvidence()
    {
        using var blocked = new BlockingSink();
        var collected = new CollectingSink();
        var composite = new CompositeOperationalSink(blocked, collected);
        OperationalRecord record = Record(EngineProtocol.Resolve, "accepted");

        composite.Write(record);

        Assert.True(blocked.Entered.Wait(
            TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        Assert.True(SpinWait.SpinUntil(
            () => collected.Records.Count == 1, TimeSpan.FromSeconds(2)));
        Assert.Same(record, Assert.Single(collected.Records));
    }

    [Fact]
    public void BlockedTelemetryCannotDelayTheLocalLogSink()
    {
        using var exporter = new BlockingExporter();
        var collected = new CollectingSink();
        var composite = new CompositeOperationalSink(
            new OperationalTelemetrySink(exporter), collected);
        OperationalRecord record = Record(EngineProtocol.Resolve, "accepted");

        composite.Write(record);

        Assert.True(SpinWait.SpinUntil(
            () => collected.Records.Count == 1, TimeSpan.FromSeconds(2)));
        Assert.Same(record, Assert.Single(collected.Records));
        Assert.True(exporter.Entered.Wait(
            TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void OperationalLogFlushDrainsTheTelemetryQueueWithinItsBudget()
    {
        var exporter = new CollectingExporter();
        var log = new OperationalLog(
            new CompositeOperationalSink(
                new CollectingSink(),
                new OperationalTelemetrySink(exporter)),
            "Marvel.Server");

        log.Write(
            OperationalEventIds.RequestCompleted,
            "accepted",
            operation: EngineProtocol.Resolve);
        log.Flush(TimeSpan.FromSeconds(2));

        Assert.Single(exporter.Envelopes);
    }

    [Fact]
    public async Task CompositeFlushPassesOnlyItsRemainingBudgetToANestedSink()
    {
        using var sink = new DelayedFlushSink();
        var composite = new CompositeOperationalSink(sink);
        composite.Write(Record(EngineProtocol.Resolve, "accepted"));
        Assert.True(sink.Entered.Wait(
            TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        Task release = Task.Run(async () =>
        {
            await Task.Delay(200, TestContext.Current.CancellationToken);
            sink.Release.Set();
        }, TestContext.Current.CancellationToken);

        ((IOperationalFlushable)composite).Flush(TimeSpan.FromSeconds(1));
        await release;

        Assert.InRange(sink.FlushBudget, TimeSpan.Zero, TimeSpan.FromMilliseconds(900));
    }

    [Fact]
    public void RestoredSessionPublishesAFreshGaugeInsteadOfALifetimeDelta()
    {
        var firstExporter = new CollectingExporter();
        var restartedExporter = new CollectingExporter();
        var first = new OperationalTelemetrySink(firstExporter);
        var restarted = new OperationalTelemetrySink(restartedExporter);

        first.Write(Record(EngineProtocol.Open, "accepted"));
        restarted.Write(Record(
            operation: null,
            disposition: "accepted",
            eventId: OperationalEventIds.SessionRestored));
        OperationalTelemetrySink.Flush(TimeSpan.FromSeconds(2));

        Assert.Equal(1, Assert.Single(
            firstExporter.Envelopes.SelectMany(value => value.Metrics),
            metric => metric.Name == "marvel.sessions.active").Value);
        Assert.Equal(1, Assert.Single(
            restartedExporter.Envelopes.SelectMany(value => value.Metrics),
            metric => metric.Name == "marvel.sessions.active").Value);
    }

    private static OperationalRecord Record(
        string? operation,
        string disposition,
        string eventId = OperationalEventIds.RequestCompleted,
        bool? saveCommitted = null,
        bool? replayVerified = null,
        string? errorCode = null,
        string? requestId = null) =>
        new(
            eventId,
            "Marvel.Server",
            DateTimeOffset.UnixEpoch,
            1,
            17,
            disposition,
            RequestId: requestId,
            GameId: "game-correlation",
            Operation: operation,
            SaveCommitted: saveCommitted,
            ReplayVerified: replayVerified,
            ReplayDiverged: errorCode == "replay_diverged",
            SessionRetired: operation == EngineProtocol.Close,
            ErrorCode: errorCode);

    private sealed class CollectingExporter : ITelemetryExporter
    {
        private readonly ConcurrentQueue<TelemetryEnvelope> envelopes = new();

        public IReadOnlyList<TelemetryEnvelope> Envelopes => [.. envelopes];

        public void Export(TelemetryEnvelope envelope) => envelopes.Enqueue(envelope);
    }

    private sealed class CollectingSink : IOperationalSink
    {
        private readonly ConcurrentQueue<OperationalRecord> records = new();

        public IReadOnlyList<OperationalRecord> Records => [.. records];

        public void Write(OperationalRecord record) => records.Enqueue(record);
    }

    private sealed class BlockingSink : IOperationalSink, IDisposable
    {
        public ManualResetEventSlim Entered { get; } = new(false);

        private ManualResetEventSlim Release { get; } = new(false);

        public void Write(OperationalRecord record)
        {
            Entered.Set();
            Release.Wait(TestContext.Current.CancellationToken);
        }

        public void Dispose()
        {
            Release.Set();
            Entered.Dispose();
            Release.Dispose();
        }
    }

    private sealed class DelayedFlushSink : IOperationalSink, IOperationalFlushable, IDisposable
    {
        public ManualResetEventSlim Entered { get; } = new(false);

        public ManualResetEventSlim Release { get; } = new(false);

        public TimeSpan FlushBudget { get; private set; } = TimeSpan.MaxValue;

        public void Write(OperationalRecord record)
        {
            Entered.Set();
            Release.Wait(TestContext.Current.CancellationToken);
        }

        public void Flush(TimeSpan timeout) => FlushBudget = timeout;

        public void Dispose()
        {
            Release.Set();
            Entered.Dispose();
            Release.Dispose();
        }
    }

    private sealed class ThrowingSink : IOperationalSink
    {
        public void Write(OperationalRecord record) => throw new IOException("private");
    }

    private sealed class RecordingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }

    private sealed class BlockingExporter : ITelemetryExporter, IDisposable
    {
        public ManualResetEventSlim Entered { get; } = new(false);

        private ManualResetEventSlim Release { get; } = new(false);

        public void Export(TelemetryEnvelope envelope)
        {
            Entered.Set();
            Release.Wait(TestContext.Current.CancellationToken);
        }

        public void Dispose()
        {
            Release.Set();
            Entered.Dispose();
            Release.Dispose();
        }
    }
}
