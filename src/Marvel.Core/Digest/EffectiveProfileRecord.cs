using System.Text.Json;

namespace Marvel.Core.Digest;

/// <summary>Complete declarative characteristics of a temporary identity.</summary>
public sealed record EffectiveProfileRecord(
    string Id, string Title, string Kind, IReadOnlyList<string> Traits,
    IReadOnlyDictionary<string, long> BaseValues)
{
    internal void WriteTo(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("id", Id);
        writer.WriteString("title", Title);
        writer.WriteString("kind", Kind);
        writer.WriteStartArray("traits");
        foreach (string trait in Traits) writer.WriteStringValue(trait);
        writer.WriteEndArray();
        writer.WriteStartObject("base_values");
        foreach (var (key, value) in BaseValues.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            writer.WriteNumber(key, value);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    internal static EffectiveProfileRecord? Read(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null) return null;
        return new(element.GetProperty("id").GetString()!, element.GetProperty("title").GetString()!,
            element.GetProperty("kind").GetString()!,
            element.GetProperty("traits").EnumerateArray().Select(trait => trait.GetString()!).ToArray(),
            element.GetProperty("base_values").EnumerateObject()
                .ToDictionary(value => value.Name, value => value.Value.GetInt64(), StringComparer.Ordinal));
    }
}
