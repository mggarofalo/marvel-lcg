using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>One bounded metric observation derived from an operational record.</summary>
public sealed record TelemetryMetric(
    string Name,
    string Kind,
    long Value,
    IReadOnlyDictionary<string, string> Dimensions);

/// <summary>One redacted span derived from an operational record.</summary>
public sealed record TelemetrySpan(
    string Name,
    string TraceId,
    DateTimeOffset StartedUtc,
    long DurationMilliseconds,
    IReadOnlyDictionary<string, string> Attributes);

/// <summary>The versioned unit sent to an explicitly configured exporter.</summary>
public sealed record TelemetryEnvelope(
    int Schema,
    IReadOnlyList<TelemetryMetric> Metrics,
    IReadOnlyList<TelemetrySpan> Spans);

/// <summary>An external consumer of already-redacted telemetry.</summary>
public interface ITelemetryExporter
{
    /// <summary>Exports one envelope without gameplay authority.</summary>
    void Export(TelemetryEnvelope envelope);
}

/// <summary>The default exporter; it deliberately does nothing.</summary>
public sealed class NoOpTelemetryExporter : ITelemetryExporter
{
    /// <inheritdoc />
    public void Export(TelemetryEnvelope envelope) { }
}

/// <summary>
/// Converts structured operational outcomes into bounded metrics and spans.
/// </summary>
public sealed class OperationalTelemetrySink(ITelemetryExporter exporter)
    : IOperationalSink, IOperationalFlushable
{
    private readonly ITelemetryExporter exporter =
        exporter ?? throw new ArgumentNullException(nameof(exporter));
    private long activeSessions;

    /// <inheritdoc />
    public void Write(OperationalRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var dimensions = Dimensions(record);
        var metrics = new List<TelemetryMetric>();
        if (record.EventId == OperationalEventIds.RequestCompleted)
        {
            metrics.Add(new("marvel.request.outcomes", "counter", 1, dimensions));
            metrics.Add(new(
                "marvel.request.latency_ms", "histogram",
                record.DurationMilliseconds, dimensions));
        }

        bool opened = record.EventId == OperationalEventIds.RequestCompleted
            && record.Operation == EngineProtocol.Open
            && record.Disposition == "accepted";
        bool restored = record.EventId == OperationalEventIds.SessionRestored
            && record.Disposition == "accepted";
        bool closed = record.SessionRetired is true;
        if (opened || restored || closed)
        {
            long current = closed
                ? DecrementActiveSessions()
                : Interlocked.Increment(ref activeSessions);
            metrics.Add(new(
                "marvel.sessions.active", "gauge", current,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["process"] = record.Process,
                }));
        }

        AddCounter(metrics, "marvel.saves.committed", dimensions,
            record.SaveCommitted is true
            && record.EventId is OperationalEventIds.PersistenceCompleted
                or OperationalEventIds.SessionRestored);
        AddCounter(metrics, "marvel.sessions.reconnects", dimensions,
            record.EventId == OperationalEventIds.ReconnectCompleted
            && record.Disposition == "accepted");
        AddCounter(metrics, "marvel.replay.divergences", dimensions,
            record.ReplayDiverged is true
            && record.EventId is OperationalEventIds.ReplayCompleted
                or OperationalEventIds.SessionRestoreFailed);
        AddCounter(metrics, "marvel.undo.refusals", dimensions,
            record.Operation == EngineProtocol.Undo && record.Disposition != "accepted");
        AddCounter(metrics, "marvel.trace_rewrites.accepted", dimensions,
            record.Operation == EngineProtocol.Reorder && record.Disposition == "accepted");

        string traceId = TraceCorrelation(record);
        var attributes = new Dictionary<string, string>(dimensions, StringComparer.Ordinal)
        {
            ["save_committed"] = (record.SaveCommitted is true).ToString(),
            ["replay_verified"] = (record.ReplayVerified is true).ToString(),
            ["replay_diverged"] = (record.ReplayDiverged is true).ToString(),
        };
        DateTimeOffset started =
            record.TimestampUtc - TimeSpan.FromMilliseconds(record.DurationMilliseconds);
        TelemetryDispatcher.Enqueue(exporter, new TelemetryEnvelope(
            1,
            metrics,
            [new TelemetrySpan(
                record.EventId, traceId, started,
                record.DurationMilliseconds, attributes)]));
    }

    private static Dictionary<string, string> Dimensions(OperationalRecord record)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["process"] = record.Process,
            ["event_id"] = record.EventId,
            ["disposition"] = record.Disposition,
        };
        if (record.Operation is not null)
        {
            result["operation"] = record.Operation;
        }
        if (record.ErrorCode is not null)
        {
            result["error_code"] = record.ErrorCode;
        }
        return result;
    }

    private static void AddCounter(
        List<TelemetryMetric> metrics,
        string name,
        IReadOnlyDictionary<string, string> dimensions,
        bool observed)
    {
        if (observed)
        {
            metrics.Add(new(name, "counter", 1, dimensions));
        }
    }

    private static string TraceCorrelation(OperationalRecord record)
    {
        string value = record.RequestId ?? record.GameId
            ?? string.Join('|', record.Process, record.ProcessId, record.EventId,
                record.TimestampUtc.UtcTicks);

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(digest.AsSpan(0, 16)).ToLowerInvariant();
    }

    internal static void Flush(TimeSpan timeout) => TelemetryDispatcher.Flush(timeout);

    void IOperationalFlushable.Flush(TimeSpan timeout) =>
        TelemetryDispatcher.Flush(timeout);

    private long DecrementActiveSessions()
    {
        while (true)
        {
            long observed = Volatile.Read(ref activeSessions);
            long updated = Math.Max(0, observed - 1);
            if (Interlocked.CompareExchange(ref activeSessions, updated, observed) == observed)
            {
                return updated;
            }
        }
    }

    private static class TelemetryDispatcher
    {
        private const int MaximumPendingExports = 1024;
        private static readonly ConcurrentQueue<Export> Pending = new();
        private static readonly AutoResetEvent Ready = new(false);
        private static int pendingCount;

        static TelemetryDispatcher()
        {
            var worker = new Thread(Drain)
            {
                IsBackground = true,
                Name = "Marvel telemetry export",
            };
            worker.Start();
        }

        public static void Enqueue(ITelemetryExporter exporter, TelemetryEnvelope envelope)
        {
            if (Interlocked.Increment(ref pendingCount) > MaximumPendingExports)
            {
                Interlocked.Decrement(ref pendingCount);
                return;
            }

            Pending.Enqueue(new Export(exporter, envelope));
            Ready.Set();
        }

        public static void Flush(TimeSpan timeout) =>
            _ = SpinWait.SpinUntil(
                () => Volatile.Read(ref pendingCount) == 0,
                timeout);

        private static void Drain()
        {
            while (true)
            {
                Ready.WaitOne();
                while (Pending.TryDequeue(out Export? pending))
                {
                    try
                    {
                        pending.Exporter.Export(pending.Envelope);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        Interlocked.Decrement(ref pendingCount);
                    }
                }
            }
        }

        private sealed record Export(
            ITelemetryExporter Exporter, TelemetryEnvelope Envelope);
    }
}

/// <summary>Posts JSON telemetry once per envelope without retrying.</summary>
public sealed class HttpTelemetryExporter : ITelemetryExporter, IDisposable
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
    private readonly HttpClient client;
    private readonly Uri endpoint;

    /// <summary>Creates an exporter for an operator-approved endpoint.</summary>
    public HttpTelemetryExporter(Uri endpoint, HttpMessageHandler? handler = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (!IsAllowedEndpoint(endpoint))
        {
            throw new ArgumentException(
                "telemetry endpoint requires HTTPS or loopback HTTP without credentials, query, or fragment",
                nameof(endpoint));
        }

        this.endpoint = endpoint;
        client = handler is null
            ? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            : new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(2);
    }

    /// <summary>Checks the transport and credential boundary used by exporters.</summary>
    public static bool IsAllowedEndpoint(Uri endpoint) =>
        endpoint.IsAbsoluteUri
        && endpoint.OriginalString.Length <= 2048
        && endpoint.UserInfo.Length == 0
        && endpoint.Query.Length == 0
        && endpoint.Fragment.Length == 0
        && endpoint.Scheme is "https" or "http"
        && (endpoint.Scheme != "http" || endpoint.IsLoopback);

    /// <inheritdoc />
    public void Export(TelemetryEnvelope envelope)
    {
        using HttpResponseMessage response = client.PostAsJsonAsync(
            endpoint, envelope, Options).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public void Dispose() => client.Dispose();
}

/// <summary>Delivers one record independently to each configured observer.</summary>
public sealed class CompositeOperationalSink(params IOperationalSink[] sinks)
    : IOperationalSink, IOperationalFlushable
{
    private readonly IReadOnlyList<IOperationalSink> sinks =
        sinks ?? throw new ArgumentNullException(nameof(sinks));

    /// <inheritdoc />
    public void Write(OperationalRecord record)
    {
        foreach (IOperationalSink sink in sinks)
        {
            try
            {
                sink.Write(record);
            }
            catch (Exception)
            {
            }
        }
    }

    void IOperationalFlushable.Flush(TimeSpan timeout)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        foreach (IOperationalFlushable sink in sinks.OfType<IOperationalFlushable>())
        {
            TimeSpan remaining = timeout - elapsed.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                return;
            }

            sink.Flush(remaining);
        }
    }
}
