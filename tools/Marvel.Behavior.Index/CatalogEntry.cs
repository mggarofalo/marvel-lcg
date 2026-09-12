using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Tests;

namespace Marvel.Behavior.Index;

internal sealed record CatalogEntry(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("fingerprint")] string Fingerprint,
    [property: JsonPropertyName("scope")] string Scope,
    [property: JsonPropertyName("disposition")] string Disposition,
    [property: JsonPropertyName("obligations")] IReadOnlyList<CatalogObligation> Obligations);
