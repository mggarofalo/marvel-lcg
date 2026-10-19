using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>Strict, deterministic JSON for the canonical save document.</summary>
public static class SessionSaveJson
{
    /// <summary>The strict snake-case serialization contract for schema 7.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    /// <summary>Validates and writes one canonical save document.</summary>
    public static string Write(SessionSave save)
    {
        Validate(save);
        return JsonSerializer.Serialize(save, Options);
    }

    /// <summary>Strictly parses and validates one canonical save document.</summary>
    public static SessionSave Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            var save = JsonSerializer.Deserialize<SessionSave>(json, Options)
                ?? throw new SessionSaveException("save contains no session document");
            Validate(save);
            return save;
        }
        catch (Exception failure) when (failure is JsonException
            or FormatException
            or InvalidOperationException
            or NotSupportedException
            or ArgumentException)
        {
            throw new SessionSaveException("save is not valid schema JSON", failure);
        }
    }

    /// <summary>Rejects unsupported identities and structurally invalid history.</summary>
    public static void Validate(SessionSave save) => SessionSaveValidation.Validate(save);

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new ResourceAllocationJsonConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
