using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>The deterministic save plus separate protected operational authority.</summary>
public sealed record StoredSession(
    [property: JsonRequired] SessionSave Save,
    [property: JsonRequired] IReadOnlyList<StoredAuthority> Authorities);
