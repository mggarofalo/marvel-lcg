using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>A non-secret verifier and its server-authorized visibility scope.</summary>
public sealed record StoredAuthority(
    [property: JsonRequired] string Verifier,
    [property: JsonRequired] IReadOnlyList<int> Seats,
    [property: JsonRequired] bool Owner,
    [property: JsonRequired] bool Invitation);
