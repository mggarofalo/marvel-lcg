using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Tests;

namespace Marvel.Behavior.Index;

internal sealed record CatalogFile(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("contract")] string Contract,
    [property: JsonPropertyName("sources")] IReadOnlyList<CatalogEntry> Sources);
