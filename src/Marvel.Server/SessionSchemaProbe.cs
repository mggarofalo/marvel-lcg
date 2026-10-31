using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>Recognizes an unreadable save that is well-formed under another schema.</summary>
internal static class SessionSchemaProbe
{
    /// <summary>Reads one generation's files and probes them; unreadable files are not a schema mismatch.</summary>
    internal static RefusedSchema? Read(string directory, string generation)
    {
        try
        {
            return Refusal(
                FileSessionStore.ReadStrict(Path.Combine(directory, generation + ".session.json")),
                FileSessionStore.ReadStrict(Path.Combine(directory, generation + ".authority.json")));
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException or SessionSaveException)
        {
            return null;
        }
    }

    private static int? Schema(JsonObject document) =>
        document["schema"] is JsonValue value && value.TryGetValue(out int schema) ? schema : null;

    private static string? Label(JsonObject document) =>
        document["session"]?["label"] is JsonValue value && value.TryGetValue(out string? label)
            && !string.IsNullOrEmpty(label) ? label : null;

    /// <summary>Returns the holders of a save under another schema, or null for any other failure.</summary>
    internal static RefusedSchema? Refusal(string save, string authority)
    {
        try
        {
            if (JsonNode.Parse(save) is not JsonObject document
                || Schema(document) is not { } schema || schema == SessionSave.CurrentSchema
                || Label(document) is not { } label)
                return null;
            // The engine's choice: a higher schema can only come from a newer product.
            string category = schema > SessionSave.CurrentSchema ? "unsupported_downgrade" : "save_schema_mismatch";
            return new RefusedSchema(label, StoredAuthorityJson.Read(authority), category);
        }
        catch (Exception failure) when (failure is JsonException or SessionSaveException
            or InvalidOperationException or FormatException)
        {
            return null;
        }
    }
}
