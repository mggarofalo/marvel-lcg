using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>

/// <summary>Strict schema JSON for one per-cost resource allocation.</summary>
public sealed class ResourceAllocationJsonConverter : JsonConverter<ResourceAllocation>
{
    /// <inheritdoc />
    public override ResourceAllocation Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("resource allocation must be an object");
        }

        (int? source, int? cost, string? paidAs) = ReadMembers(root);
        if (source is null || cost is null || paidAs is null)
        {
            throw new JsonException("resource allocation is missing a required member");
        }
        return new ResourceAllocation(source.Value, cost.Value, paidAs);
    }

    private static (int? Source, int? Cost, string? PaidAs) ReadMembers(JsonElement root)
    {
        int? source = null;
        int? cost = null;
        string? paidAs = null;
        foreach (JsonProperty property in root.EnumerateObject())
        {
            switch (property.Name)
            {
                case "source" when source is null:
                    source = property.Value.GetInt32();
                    break;
                case "cost" when cost is null:
                    cost = property.Value.GetInt32();
                    break;
                case "paid_as" when paidAs is null:
                    paidAs = property.Value.GetString()
                        ?? throw new JsonException("resource allocation paid_as is null");
                    break;
                default:
                    throw new JsonException(
                        $"resource allocation member '{property.Name}' is not allowed");
            }
        }
        return (source, cost, paidAs);
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        ResourceAllocation value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("source", value.Source);
        writer.WriteNumber("cost", value.Cost);
        writer.WriteString("paid_as", value.PaidAs);
        writer.WriteEndObject();
    }
}
