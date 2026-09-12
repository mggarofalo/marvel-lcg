using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Tests;

namespace Marvel.Behavior.Index;

internal sealed record Adjudication(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("fingerprint")] string Fingerprint,
    [property: JsonPropertyName("obligations")] IReadOnlyList<Obligation> Obligations);
