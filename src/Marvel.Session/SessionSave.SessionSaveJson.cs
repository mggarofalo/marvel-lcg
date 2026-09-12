using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>Replay identities selected by this engine build and its datasets.</summary>

/// <summary>Strict, deterministic JSON for the canonical save document.</summary>
public static class SessionSaveJson
{
    /// <summary>The strict snake-case serialization contract for schema 3.</summary>
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
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("schema", out JsonElement schema)
                && schema.ValueKind == JsonValueKind.Number
                && schema.GetInt32() == 2)
            {
                return ReadSchemaTwo(json);
            }

            var save = JsonSerializer.Deserialize<SessionSave>(json, Options)
                ?? throw new SessionSaveException("save contains no session document");
            Validate(save);
            return save;
        }
        catch (Exception failure) when (failure is JsonException
            or FormatException
            or InvalidOperationException
            or NotSupportedException)
        {
            throw new SessionSaveException("save is not valid schema JSON", failure);
        }
    }

    /// <summary>Validates either the current schema or the one migratable predecessor.</summary>
    public static void ValidateReadable(SessionSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Schema == 2)
        {
            ValidateSchemaTwo(save);
            return;
        }

        Validate(save);
    }

    /// <summary>Rejects unsupported identities and structurally invalid history.</summary>
    public static void Validate(SessionSave save)
        => SessionSaveValidation.Validate(save);

    private static SessionSave ReadSchemaTwo(string json)
    {
        var legacy = JsonSerializer.Deserialize<SchemaTwoSessionSave>(json, Options)
            ?? throw new SessionSaveException("save contains no session document");
        if (legacy.Units is null)
        {
            throw new JsonException("schema 2 units are null");
        }

        var save = new SessionSave(
            legacy.Format,
            legacy.Schema,
            legacy.Compatibility,
            legacy.Session,
            legacy.Setup,
            legacy.Initial,
            legacy.Revision,
            legacy.Cursor,
            legacy.EditFrontier,
            ReadSchemaTwoPrompt(legacy.CurrentPrompt),
            [.. legacy.Units.Select(unit => ConvertSchemaTwoUnit(
                unit ?? throw new JsonException("schema 2 unit is null")))]);
        ValidateSchemaTwo(save);
        return save;
    }

    private static JournalUnit ConvertSchemaTwoUnit(SchemaTwoJournalUnit unit)
    {
        if (unit.Decisions is null)
        {
            throw new JsonException("schema 2 unit decisions are null");
        }

        return new JournalUnit(
            unit.Role,
            unit.Status,
            unit.InitiatingSeat,
            unit.ActiveSeat,
            unit.Round,
            unit.Phase,
            [.. unit.Decisions.Select(step => ConvertSchemaTwoStep(
                step ?? throw new JsonException("schema 2 step is null")))],
            unit.Exposures);
    }

    private static JournalStep ConvertSchemaTwoStep(SchemaTwoJournalStep step) =>
        new(
            SchemaTwoPromptJson.Read(step.Prompt),
            step.Decision,
            step.Events,
            step.RngWords,
            step.StateFingerprint,
            step.Result);

    private static PromptRecord? ReadSchemaTwoPrompt(JsonElement? prompt) =>
        prompt is null || prompt.Value.ValueKind == JsonValueKind.Null
            ? null
            : SchemaTwoPromptJson.Read(prompt.Value);

    private static void ValidateSchemaTwo(SessionSave save)
    {
        if (save.Schema != 2)
        {
            throw new SessionSaveException("schema 2 save is not migratable");
        }

        Validate(save with { Schema = SessionSave.CurrentSchema });
    }

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
