using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Tests;

namespace Marvel.Behavior.Index;

internal sealed record AdjudicationFile(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("sources")] IReadOnlyList<Adjudication> Sources);
