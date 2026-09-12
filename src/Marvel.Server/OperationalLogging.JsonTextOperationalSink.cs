using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Marvel.Server;

/// <summary>Stable identifiers for machine-readable operational events.</summary>

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
