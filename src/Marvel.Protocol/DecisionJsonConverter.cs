using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Play;

namespace Marvel.Protocol;

/// <summary>Admits the five answer fields without serializing engine convenience accessors.</summary>
public sealed class DecisionJsonConverter : JsonConverter<Decision>
{
    /// <inheritdoc />
    public override Decision Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new JsonException("A decision must be an object.");
        ValidateFields(root);
        (int id, JsonElement targets) = RequiredAnswer(root);
        return new Decision(id,
            targets.Deserialize(EngineJsonContext.Default.Int32Array)!,
            root.TryGetProperty("resources", out var resources)
                ? resources.Deserialize(EngineJsonContext.Default.Int32Array) : null,
            root.TryGetProperty("values", out var values)
                ? values.Deserialize(EngineJsonContext.Default.DictionaryStringInt64) : null,
            root.TryGetProperty("allocations", out var allocations)
                ? allocations.Deserialize(EngineJsonContext.Default.ResourceAllocationArray) : null);
    }

    private static void ValidateFields(JsonElement root)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (property.Name is not ("affordance" or "targets" or "resources" or "values" or "allocations")
                || !fields.Add(property.Name)) throw new JsonException("Unknown or duplicate decision field.");
        }
    }

    private static (int Id, JsonElement Targets) RequiredAnswer(JsonElement root)
    {
        if (!root.TryGetProperty("affordance", out var affordance)
            || affordance.ValueKind != JsonValueKind.Number || !affordance.TryGetInt32(out int id)
            || !root.TryGetProperty("targets", out var targets) || targets.ValueKind != JsonValueKind.Array)
            throw new JsonException("A decision requires an affordance and target array.");
        return (id, targets);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Decision value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("affordance", value.Affordance);
        writer.WritePropertyName("targets");
        if (value.Targets is null) writer.WriteNullValue();
        else JsonSerializer.Serialize(writer, value.Targets.ToArray(), EngineJsonContext.Default.Int32Array);
        writer.WritePropertyName("resources");
        if (value.Resources is null) writer.WriteNullValue();
        else JsonSerializer.Serialize(writer, value.Resources.ToArray(), EngineJsonContext.Default.Int32Array);
        writer.WritePropertyName("values");
        if (value.Values is null) writer.WriteNullValue();
        else JsonSerializer.Serialize(writer, new Dictionary<string, long>(value.Values),
            EngineJsonContext.Default.DictionaryStringInt64);
        writer.WritePropertyName("allocations");
        if (value.Allocations is null) writer.WriteNullValue();
        else JsonSerializer.Serialize(writer, value.Allocations.ToArray(), EngineJsonContext.Default.ResourceAllocationArray);
        writer.WriteEndObject();
    }
}
