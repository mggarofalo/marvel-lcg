using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>Stable identifiers for machine-readable operational events.</summary>
public static class OperationalEventIds
{
    /// <summary>One engine protocol request reached a final disposition.</summary>
    public const string RequestCompleted = "server.request.completed";

    /// <summary>One durable session was verified and published during startup.</summary>
    public const string SessionRestored = "session.restore.completed";

    /// <summary>Startup rejected a durable session before publication.</summary>
    public const string SessionRestoreFailed = "session.restore.failed";

    /// <summary>The standalone socket listener began accepting connections.</summary>
    public const string ServerListening = "server.listener.started";

    /// <summary>The process could not start its server composition.</summary>
    public const string ServerStartFailed = "server.start.failed";

    /// <summary>One client transport exchange reached a final disposition.</summary>
    public const string TransportCompleted = "transport.exchange.completed";
}

/// <summary>
/// One bounded operational outcome. It deliberately has no field capable of
/// carrying a capability, invitation, card, payment, save body, or exception.
/// </summary>
public sealed record OperationalRecord(
    string EventId,
    string Process,
    DateTimeOffset TimestampUtc,
    int ProcessId,
    long DurationMilliseconds,
    string Disposition,
    string? RequestId = null,
    string? GameId = null,
    string? Operation = null,
    long? Revision = null,
    int? AuthorizedSeat = null,
    bool? SaveCommitted = null,
    bool? ReplayVerified = null,
    string? ErrorCode = null);

/// <summary>A destination for already-redacted structured operational records.</summary>
public interface IOperationalSink
{
    /// <summary>Consumes one complete record.</summary>
    void Write(OperationalRecord record);
}

/// <summary>
/// The single safe boundary between session orchestration and operational I/O.
/// </summary>
public sealed class OperationalLog
{
    private const int MaximumFieldLength = EngineProtocol.MaximumIdentifierLength;
    private readonly Func<DateTimeOffset> clock;
    private readonly IOperationalSink? sink;
    private readonly string process;

    /// <summary>A disabled observer that performs no I/O.</summary>
    public static OperationalLog None { get; } = new(null, "disabled");

    /// <summary>Creates a failure-isolated logger for one process composition.</summary>
    public OperationalLog(
        IOperationalSink? sink,
        string process,
        Func<DateTimeOffset>? clock = null)
    {
        this.sink = sink;
        this.process = Bound(process);
        this.clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>Writes one record without allowing observation to affect gameplay.</summary>
    public void Write(
        string eventId,
        string disposition,
        long durationMilliseconds = 0,
        string? requestId = null,
        string? gameId = null,
        string? operation = null,
        long? revision = null,
        int? authorizedSeat = null,
        bool? saveCommitted = null,
        bool? replayVerified = null,
        string? errorCode = null)
    {
        if (sink is null)
        {
            return;
        }

        try
        {
            var record = new OperationalRecord(
                Bound(eventId),
                process,
                clock(),
                Environment.ProcessId,
                Math.Max(0, durationMilliseconds),
                Bound(disposition),
                Correlation(requestId),
                Correlation(gameId),
                SafeOperation(operation),
                revision,
                authorizedSeat,
                saveCommitted,
                replayVerified,
                SafeErrorCode(errorCode));
            Dispatcher.Enqueue(sink, record);
        }
        catch (Exception)
        {
            // Observation is deliberately best-effort. A broken local or
            // remote sink cannot fail, retry, delay, or reorder gameplay.
        }
    }

    /// <summary>Waits only up to the supplied operational shutdown budget.</summary>
    public void Flush(TimeSpan timeout)
    {
        if (sink is null || timeout <= TimeSpan.Zero)
        {
            return;
        }

        Dispatcher.Flush(timeout);
    }

    private static string Bound(string? value) =>
        string.IsNullOrEmpty(value)
            ? "unknown"
            : value[..Math.Min(value.Length, MaximumFieldLength)];

    private static string? SafeOperation(string? value) => value switch
    {
        null => null,
        EngineProtocol.Setup or EngineProtocol.Open or EngineProtocol.Attach
            or EngineProtocol.Sync or EngineProtocol.Resolve or EngineProtocol.Undo
            or EngineProtocol.Redo or EngineProtocol.Reorder or EngineProtocol.Close
            or "listen" or "start" or "embedded_start" => value,
        _ => "unknown",
    };

    private static string? SafeErrorCode(string? value) => value switch
    {
        null => null,
        "content_unavailable" or "engine_error" or "game_aborted"
            or "history_authority" or "history_direction" or "history_failed"
            or "history_frontier" or "history_open"
            or "invalid_decision" or "invalid_frame" or "invalid_request"
            or "not_your_turn" or "reorder_failed" or "reorder_kind"
            or "reorder_shape" or "response_failed"
            or "restore_failed" or "save_failed" or "server_start_failed"
            or "session_not_found" or "setup_unavailable" or "stale_decision"
            or "stale_history" or "transport_cancelled" or "transport_failed"
            or "unsupported_version" => value,
        _ => "unknown_error",
    };

    private static string? Correlation(string? value)
    {
        if (value is null)
        {
            return null;
        }

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(digest.AsSpan(0, 16)).ToLowerInvariant();
    }

    private static class Dispatcher
    {
        private const int MaximumPendingRecords = 1024;
        private static readonly ConcurrentQueue<Delivery> Pending = new();
        private static readonly AutoResetEvent Ready = new(false);
        private static int pendingCount;

        static Dispatcher()
        {
            var worker = new Thread(Drain)
            {
                IsBackground = true,
                Name = "Marvel operational log",
            };
            worker.Start();
        }

        public static void Enqueue(IOperationalSink sink, OperationalRecord record)
        {
            if (Interlocked.Increment(ref pendingCount) > MaximumPendingRecords)
            {
                Interlocked.Decrement(ref pendingCount);
                return;
            }

            Pending.Enqueue(new Delivery(sink, record));
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
                while (Pending.TryDequeue(out Delivery? delivery))
                {
                    try
                    {
                        delivery.Sink.Write(delivery.Record);
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

        private sealed record Delivery(
            IOperationalSink Sink, OperationalRecord Record);
    }
}

/// <summary>Writes the same structured records as one JSON object per line.</summary>
public sealed class JsonTextOperationalSink(TextWriter writer) : IOperationalSink
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly TextWriter writer =
        writer ?? throw new ArgumentNullException(nameof(writer));

    /// <inheritdoc />
    public void Write(OperationalRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        writer.WriteLine(JsonSerializer.Serialize(record, Options));
        writer.Flush();
    }
}
