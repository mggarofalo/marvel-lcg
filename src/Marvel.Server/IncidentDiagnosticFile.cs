using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>A retained log file and its selected already-redacted records.</summary>
public sealed record IncidentDiagnosticFile(
    string Name,
    long Bytes,
    string Sha256,
    int InvalidRecords,
    IReadOnlyList<OperationalRecord> Records);
