using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Marvel.Server;

/// <summary>Stable identifiers for machine-readable operational events.</summary>

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
        bool? replayDiverged = null,
        bool? sessionRetired = null,
        string? errorCode = null,
        long? expectedRevision = null,
        string? saveGeneration = null,
        string? stage = null)
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
                replayDiverged,
                sessionRetired,
                SafeErrorCode(errorCode),
                EngineBuildIdentity.ProductVersion,
                EngineBuildIdentity.Commit,
                EngineBuildIdentity.Display,
                expectedRevision,
                SafeGeneration(saveGeneration),
                SafeStage(stage));
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

        var elapsed = Stopwatch.StartNew();
        Dispatcher.Flush(timeout);
        TimeSpan remaining = timeout - elapsed.Elapsed;
        if (remaining > TimeSpan.Zero && sink is IOperationalFlushable flushable)
        {
            flushable.Flush(remaining);
        }
    }

    private static string Bound(string? value) =>
        string.IsNullOrEmpty(value)
            ? "unknown"
            : value[..Math.Min(value.Length, MaximumFieldLength)];

    internal static string? SafeOperation(string? value) => value switch
    {
        null => null,
        EngineProtocol.Setup or EngineProtocol.Open or EngineProtocol.Attach
            or EngineProtocol.Sync or EngineProtocol.Resolve or EngineProtocol.Undo
            or EngineProtocol.Redo or EngineProtocol.Reorder or EngineProtocol.Close
            or "listen" or "start" or "embedded_start" or "reconnect"
            or "replay" or "persistence" => value,
        _ => "unknown",
    };

    internal static string? SafeErrorCode(string? value) => value switch
    {
        null => null,
        "content_unavailable" or "engine_error" or "game_aborted"
            or "history_authority" or "history_direction" or "history_failed"
            or "history_frontier" or "history_open"
            or "invalid_decision" or "invalid_frame" or "invalid_request"
            or "not_your_turn" or "reorder_failed" or "reorder_kind"
            or "reorder_shape" or "response_failed"
            or "persistence_failed" or "replay_diverged" or "replay_failed"
            or "restore_failed" or "save_failed"
            or "server_start_failed"
            or "diagnostics_unavailable"
            or "session_not_found" or "setup_unavailable" or "stale_decision"
            or "stale_history" or "transport_cancelled" or "transport_failed"
            or "unsupported_version"
            or "unsupported_downgrade"
            or "replay_identity_mismatch"
            or "rng_identity_mismatch"
            or "digest_identity_mismatch"
            or "cards_dataset_mismatch"
            or "setup_dataset_mismatch"
            or "abilities_dataset_mismatch" => value,
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

    internal static string? SafeGeneration(string? value) =>
        value is { Length: 32 }
        && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f')
            ? value
            : value is null ? null : "unknown";

    internal static string? SafeStage(string? value) => value switch
    {
        null or "restore" or "migration" or "quarantine" => value,
        _ => "unknown",
    };

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
