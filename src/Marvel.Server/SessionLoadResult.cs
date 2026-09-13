using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>One independently loadable session or its bounded quarantine result.</summary>
public sealed record SessionLoadResult(
    StoredSession? Session,
    string? StorageId,
    string? ErrorCode,
    string? Generation = null);
