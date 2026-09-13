using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>An external consumer of already-redacted telemetry.</summary>
public interface ITelemetryExporter
{
    /// <summary>Exports one envelope without gameplay authority.</summary>
    void Export(TelemetryEnvelope envelope);
}
