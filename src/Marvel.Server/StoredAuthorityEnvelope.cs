using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>The current protected operational authority format, independent of deterministic saves.</summary>
internal sealed record StoredAuthorityEnvelope(
    [property: JsonRequired] int Schema,
    [property: JsonRequired] IReadOnlyList<StoredAuthority> Authorities);
