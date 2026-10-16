using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>A non-secret authenticated receipt; only the retained client claim can decrypt its authority.</summary>
public sealed record StoredEntryReceipt(
    [property: JsonRequired] string Verifier,
    [property: JsonRequired] string Fingerprint,
    [property: JsonRequired] string Nonce,
    [property: JsonRequired] string Ciphertext,
    [property: JsonRequired] string Tag);
