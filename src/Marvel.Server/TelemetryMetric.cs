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
