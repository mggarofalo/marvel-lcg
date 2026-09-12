using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Marvel.Server;

/// <summary>Stable identifiers for machine-readable operational events.</summary>

/// <summary>A destination for already-redacted structured operational records.</summary>
public interface IOperationalSink
{
    /// <summary>Consumes one complete record.</summary>
    void Write(OperationalRecord record);
}
