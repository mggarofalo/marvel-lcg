using System.Text.Json;
using System.Text.Json.Nodes;

namespace Marvel.Session;

/// <summary>Reads the fixed resource requirements of schema 4 prompts.</summary>
public static class SchemaFourPromptJson
{
    /// <summary>Strictly reads a predecessor prompt without quantity-dependent resource types.</summary>
    public static PromptRecord Read(JsonElement element)
    {
        JsonObject prompt = JsonNode.Parse(element.GetRawText())?.AsObject()
            ?? throw new JsonException("schema 4 prompt is null");
        AddFixedResourceCosts(prompt);
        return prompt.Deserialize<PromptRecord>(SessionSaveJson.Options)
            ?? throw new JsonException("schema 4 prompt is null");
    }

    internal static void AddFixedResourceCosts(JsonObject prompt)
    {
        foreach (JsonNode? option in Elements(prompt, "affordances"))
        foreach (JsonNode? cost in Elements(option, "costs"))
        foreach (JsonNode? component in Elements(cost, "components"))
        {
            JsonObject record = component?.AsObject()
                ?? throw new JsonException("predecessor resource component is null");
            if (record.ContainsKey("repeated_resource"))
                throw new JsonException("predecessor resource component has repeated_resource");
            record["repeated_resource"] = null;
        }
    }

    private static JsonArray Elements(JsonNode? node, string property) =>
        node?[property]?.AsArray() ?? [];
}
