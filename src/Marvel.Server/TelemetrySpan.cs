using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>One redacted span derived from an operational record.</summary>
public sealed record TelemetrySpan(
    string Name,
    string TraceId,
    DateTimeOffset StartedUtc,
    long DurationMilliseconds,
    IReadOnlyDictionary<string, string> Attributes);
