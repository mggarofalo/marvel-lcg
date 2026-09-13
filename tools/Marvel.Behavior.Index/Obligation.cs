using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Tests;

namespace Marvel.Behavior.Index;

internal sealed record Obligation(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("disposition")] string Disposition,
    [property: JsonPropertyName("reason")] string? Reason,
    [property: JsonPropertyName("target")] string? Target,
    [property: JsonPropertyName("implementation")] string? Implementation,
    [property: JsonPropertyName("work_item")] string? WorkItem,
    [property: JsonPropertyName("exception")] string? Exception,
    [property: JsonPropertyName("scenarios")] IReadOnlyList<string> Scenarios,
    [property: JsonPropertyName("mutation")] string? Mutation);
