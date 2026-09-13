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
    /// <summary>The strict snake-case serialization contract for schema 4.</summary>
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
            int? schema = ReadSchema(json);
            if (schema == 2)
            {
                return ReadSchemaTwo(json);
            }
            if (schema == 3)
            {
                return ReadSchemaThree(json);
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

    private static int? ReadSchema(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty("schema", out JsonElement value)
            && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;
    }

    /// <summary>Validates either the current schema or the one migratable predecessor.</summary>
    public static void ValidateReadable(SessionSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Schema is 2 or 3)
        {
            ValidatePredecessor(save);
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
        ValidatePredecessor(save);
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
            ReadSchemaTwoDecision(step.Decision),
            step.Events,
            step.RngWords,
            step.StateFingerprint,
            step.Result);

    private static PromptRecord? ReadSchemaTwoPrompt(JsonElement? prompt) =>
        prompt is null || prompt.Value.ValueKind == JsonValueKind.Null
            ? null
            : SchemaTwoPromptJson.Read(prompt.Value);

    private static DurableDecision ReadSchemaTwoDecision(JsonElement decision)
    {
        JsonObject root = JsonNode.Parse(decision.GetRawText())?.AsObject()
            ?? throw new JsonException("schema 2 decision is null");
        AddCardAnchorKind(root["selector"]?.AsObject()
            ?? throw new JsonException("schema 2 decision selector is null"));
        return root.Deserialize<DurableDecision>(Options)
            ?? throw new JsonException("schema 2 decision is null");
    }

    private static SessionSave ReadSchemaThree(string json)
    {
        JsonObject root = JsonNode.Parse(json)?.AsObject()
            ?? throw new JsonException("schema 3 save is null");
        AddCardAnchorKinds(root);
        SessionSave save = root.Deserialize<SessionSave>(Options)
            ?? throw new JsonException("schema 3 save is null");
        ValidatePredecessor(save);
        return save;
    }

    private static void AddCardAnchorKinds(JsonObject root)
    {
        AddCardAnchorKind(root["current_prompt"]?.AsObject());
        foreach (JsonObject record in JournalSteps(root))
        {
            AddCardAnchorKind(record["prompt"]?.AsObject());
            AddCardAnchorKind(record["decision"]?["selector"]?.AsObject());
        }
    }

    private static IEnumerable<JsonObject> JournalSteps(JsonObject root) =>
        (root["units"]?.AsArray() ?? []).SelectMany(unit =>
            unit?["decisions"]?.AsArray() ?? []).Select(step => step?.AsObject()
                ?? throw new JsonException("schema 3 journal step is null"));

    private static void AddCardAnchorKind(JsonObject? record)
    {
        if (record is null) return;
        if (record.ContainsKey("anchor_id"))
        {
            AddCardAnchorKindToSelector(record);
            return;
        }

        AddCardAnchorKindToAffordances(record);
    }

    private static void AddCardAnchorKindToSelector(JsonObject selector)
    {
        if (selector.ContainsKey("anchor_kind"))
            throw new JsonException("predecessor decision selector has anchor_kind");
        selector["anchor_kind"] = selector["decline"]?.GetValue<bool>() == true
            ? null
            : (int)AffordanceAnchorKind.Card;
    }

    private static void AddCardAnchorKindToAffordances(JsonObject prompt)
    {
        foreach (JsonNode? affordance in prompt["affordances"]?.AsArray() ?? [])
        {
            JsonObject choice = affordance?.AsObject()
                ?? throw new JsonException("schema 3 affordance is null");
            if (choice.ContainsKey("anchor_kind"))
                throw new JsonException("predecessor affordance has anchor_kind");
            choice["anchor_kind"] = (int)AffordanceAnchorKind.Card;
        }
    }

    private static void ValidatePredecessor(SessionSave save)
    {
        if (save.Schema is not (2 or 3))
        {
            throw new SessionSaveException("save schema is not migratable");
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
