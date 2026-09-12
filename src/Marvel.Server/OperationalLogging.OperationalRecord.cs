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
