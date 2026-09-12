using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Marvel.Server;

/// <summary>Stable identifiers for machine-readable operational events.</summary>

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
        ValidIdentity(record) && ValidDisposition(record) && ValidBuild(record)
        && OperationalLog.SafeGeneration(record.SaveGeneration) == record.SaveGeneration
        && OperationalLog.SafeStage(record.Stage) == record.Stage;

    private static bool ValidIdentity(OperationalRecord record) =>
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
        && record.DurationMilliseconds >= 0;

    private static bool ValidDisposition(OperationalRecord record) =>
        record.Disposition is "accepted" or "rejected" or "stale"
            or "uncertain" or "cancelled"
        && ValidCorrelation(record.RequestId)
        && ValidCorrelation(record.GameId)
        && OperationalLog.SafeOperation(record.Operation) == record.Operation
        && record.Revision is null or >= 0
        && record.ExpectedRevision is null or >= 0
        && record.AuthorizedSeat is null or >= 0
        && OperationalLog.SafeErrorCode(record.ErrorCode) == record.ErrorCode;

    private static bool ValidBuild(OperationalRecord record) =>
        record.ProductVersion is not null
        && ProductVersion.IsMatch(record.ProductVersion)
        && record.Commit is not null
        && Commit.IsMatch(record.Commit)
        && record.Runtime is not null
        && Runtime.IsMatch(record.Runtime)
        && record.Runtime.StartsWith("v" + record.ProductVersion + " · ",
            StringComparison.Ordinal);

    private static bool ValidCorrelation(string? value) =>
        value is null
        || value is { Length: 32 }
        && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
}
