using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>The versioned unit sent to an explicitly configured exporter.</summary>
public sealed record TelemetryEnvelope(
    int Schema,
    IReadOnlyList<TelemetryMetric> Metrics,
    IReadOnlyList<TelemetrySpan> Spans);
