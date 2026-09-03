using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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

    /// <summary>The standalone listener completed an intentional shutdown.</summary>
    public const string ServerStopped = "server.listener.stopped";

    /// <summary>The process could not start its server composition.</summary>
    public const string ServerStartFailed = "server.start.failed";

    /// <summary>One client transport exchange reached a final disposition.</summary>
    public const string TransportCompleted = "transport.exchange.completed";

    /// <summary>A client synchronized after an uncertain mutation outcome.</summary>
    public const string ReconnectCompleted = "client.reconnect.completed";

    /// <summary>One replay stage reached a final operational disposition.</summary>
    public const string ReplayCompleted = "session.replay.completed";

    /// <summary>One persistence stage reached a final operational disposition.</summary>
    public const string PersistenceCompleted = "session.persistence.completed";

    /// <summary>A configured durable diagnostic destination could not be used.</summary>
    public const string DiagnosticsUnavailable = "diagnostics.sink.unavailable";
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
    bool? ReplayDiverged = null,
    bool? SessionRetired = null,
    string? ErrorCode = null,
    string? ProductVersion = null,
    string? Commit = null,
    string? Runtime = null,
    long? ExpectedRevision = null,
    string? SaveGeneration = null,
    string? Stage = null);

/// <summary>A destination for already-redacted structured operational records.</summary>
public interface IOperationalSink
{
    /// <summary>Consumes one complete record.</summary>
    void Write(OperationalRecord record);
}

internal interface IOperationalFlushable
{
    void Flush(TimeSpan timeout);
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

/// <summary>Writes the same structured records as one JSON object per line.</summary>
public sealed class JsonTextOperationalSink(TextWriter writer) : IOperationalSink
{
    private readonly TextWriter writer =
        writer ?? throw new ArgumentNullException(nameof(writer));

    /// <inheritdoc />
    public void Write(OperationalRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        writer.WriteLine(OperationalJson.Serialize(record));
        writer.Flush();
    }
}

internal static class OperationalJson
{
    private static readonly Regex ProductVersion = new(
        "^[0-9]{1,5}\\.[0-9]{1,5}\\.[0-9]{1,5}(?:-[0-9A-Za-z.-]{1,32})?(?:\\+[0-9A-Za-z.-]{1,32})?$",
        RegexOptions.CultureInvariant);
    private static readonly Regex Commit = new(
        "^(?:local|[0-9a-f]{40})$",
        RegexOptions.CultureInvariant);
    private static readonly Regex Runtime = new(
        "^v[0-9A-Za-z.+-]{1,96} · engine engine-replay-v[0-9]{1,5} · protocol [0-9]{1,5} · save [0-9]{1,5}$",
        RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(OperationalRecord record) =>
        JsonSerializer.Serialize(record, Options);

    public static OperationalRecord Read(string json) =>
        JsonSerializer.Deserialize<OperationalRecord>(json, Options)
        ?? throw new JsonException("operational record is null");

    public static OperationalRecord ReadVerified(string json)
    {
        OperationalRecord record = Read(json);
        if (!string.Equals(Serialize(record), json, StringComparison.Ordinal)
            || !Valid(record))
        {
            throw new JsonException("operational record is not canonical");
        }

        return record;
    }

    private static bool Valid(OperationalRecord record) =>
        record.EventId is OperationalEventIds.RequestCompleted
            or OperationalEventIds.SessionRestored
            or OperationalEventIds.SessionRestoreFailed
            or OperationalEventIds.ServerListening
            or OperationalEventIds.ServerStopped
            or OperationalEventIds.ServerStartFailed
            or OperationalEventIds.TransportCompleted
            or OperationalEventIds.ReconnectCompleted
            or OperationalEventIds.ReplayCompleted
            or OperationalEventIds.PersistenceCompleted
            or OperationalEventIds.DiagnosticsUnavailable
        && record.Process is "Marvel.Server" or "Marvel.Godot"
        && record.ProcessId > 0
        && record.DurationMilliseconds >= 0
        && record.Disposition is "accepted" or "rejected" or "stale"
            or "uncertain" or "cancelled"
        && ValidCorrelation(record.RequestId)
        && ValidCorrelation(record.GameId)
        && OperationalLog.SafeOperation(record.Operation) == record.Operation
        && record.Revision is null or >= 0
        && record.ExpectedRevision is null or >= 0
        && record.AuthorizedSeat is null or >= 0
        && OperationalLog.SafeErrorCode(record.ErrorCode) == record.ErrorCode
        && record.ProductVersion is not null
        && ProductVersion.IsMatch(record.ProductVersion)
        && record.Commit is not null
        && Commit.IsMatch(record.Commit)
        && record.Runtime is not null
        && Runtime.IsMatch(record.Runtime)
        && record.Runtime.StartsWith("v" + record.ProductVersion + " · ",
            StringComparison.Ordinal)
        && OperationalLog.SafeGeneration(record.SaveGeneration) == record.SaveGeneration
        && OperationalLog.SafeStage(record.Stage) == record.Stage;

    private static bool ValidCorrelation(string? value) =>
        value is null
        || value is { Length: 32 }
        && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
}

/// <summary>Retains bounded private JSON-lines diagnostics beside stderr.</summary>
public sealed class RotatingJsonFileOperationalSink : IOperationalSink, IDisposable
{
    private const string ActiveName = "operational.jsonl";
    private readonly Func<DateTimeOffset> clock;
    private readonly long maximumBytes;
    private readonly int maximumArchives;
    private readonly TimeSpan retention;
    private readonly string root;
    private readonly object gate = new();
    private StreamWriter? writer;

    /// <summary>Creates a size- and age-bounded diagnostic destination.</summary>
    public RotatingJsonFileOperationalSink(
        string root,
        long maximumBytes = 10 * 1024 * 1024,
        int maximumArchives = 9,
        TimeSpan? retention = null,
        Func<DateTimeOffset>? clock = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        if (maximumBytes <= 0 || maximumArchives < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        }

        this.root = Path.GetFullPath(root);
        this.maximumBytes = maximumBytes;
        this.maximumArchives = maximumArchives;
        this.retention = retention ?? TimeSpan.FromDays(30);
        this.clock = clock ?? (() => DateTimeOffset.UtcNow);
        Directory.CreateDirectory(this.root);
        MakePrivate(this.root, directory: true);
        Prune();
        EnsureWriter();
    }

    /// <inheritdoc />
    public void Write(OperationalRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        string line = OperationalJson.Serialize(record);
        lock (gate)
        {
            EnsureWriter();
            if (writer!.BaseStream.Length > 0
                && writer.BaseStream.Length + System.Text.Encoding.UTF8.GetByteCount(line) + 1
                    > maximumBytes)
            {
                Rotate();
                EnsureWriter();
            }

            writer!.WriteLine(line);
            writer.Flush();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (gate)
        {
            writer?.Dispose();
            writer = null;
        }
    }

    private void EnsureWriter()
    {
        if (writer is not null)
        {
            return;
        }

        string path = Path.Combine(root, ActiveName);
        if (File.Exists(path))
        {
            MakePrivate(path, directory: false);
        }
        var stream = new FileStream(
            path, FileMode.Append, FileAccess.Write, FileShare.Read,
            bufferSize: 4096, FileOptions.WriteThrough);
        MakePrivate(path, directory: false);
        writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false));
    }

    private void Rotate()
    {
        writer?.Dispose();
        writer = null;
        string active = Path.Combine(root, ActiveName);
        for (int suffix = 0; ; suffix++)
        {
            string archive = Path.Combine(
                root,
                $"operational-{clock().UtcDateTime:yyyyMMddHHmmssfff}-{suffix:D3}.jsonl");
            if (!File.Exists(archive))
            {
                File.Move(active, archive);
                break;
            }
        }

        Prune();
    }

    private void Prune()
    {
        DateTimeOffset cutoff = clock() - retention;
        string[] archives = Directory.GetFiles(root, "operational-*.jsonl")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToArray();
        foreach (string archive in archives
                     .Where((path, index) => index >= maximumArchives
                         || File.GetLastWriteTimeUtc(path) < cutoff.UtcDateTime))
        {
            File.Delete(archive);
        }
    }

    private static void MakePrivate(string path, bool directory)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        File.SetUnixFileMode(
            path,
            directory
                ? UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                : UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}
