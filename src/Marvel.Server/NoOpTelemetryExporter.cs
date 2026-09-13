using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>The default exporter; it deliberately does nothing.</summary>
public sealed class NoOpTelemetryExporter : ITelemetryExporter
{
    /// <inheritdoc />
    public void Export(TelemetryEnvelope envelope) { }
}
