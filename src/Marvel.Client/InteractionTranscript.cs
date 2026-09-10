using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Client;

/// <summary>Collects the game information already authorized for one client.</summary>
public sealed class InteractionTranscript
{
    private const string Format = "marvel-client-interaction";
    private const int Schema = 2;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };
    private readonly List<InteractionTranscriptEntry> entries = [];
    private Outcome previousOutcome = Outcome.Unfinished;

    /// <summary>The complete ordered interaction history collected by this client.</summary>
    public IReadOnlyList<InteractionTranscriptEntry> Entries => entries;

    /// <summary>The deterministic seed, when this client opened the game.</summary>
    public uint? Seed { get; private set; }

    /// <summary>The runtime identity discovered from the engine.</summary>
    public RuntimeIdentity? Runtime { get; private set; }

    /// <summary>The setup known to this viewer.</summary>
    public InteractionTranscriptSetup Setup { get; private set; } =
        InteractionTranscriptSetup.Unavailable("not_recorded");

    /// <summary>Starts a fresh transcript without retaining a prior table.</summary>
    public void Reset(
        uint? seed,
        RuntimeIdentity? runtime,
        InteractionTranscriptSetup? setup = null)
    {
        Seed = seed;
        Runtime = runtime;
        Setup = setup ?? InteractionTranscriptSetup.Unavailable("not_recorded");
        previousOutcome = Outcome.Unfinished;
        entries.Clear();
    }

    /// <summary>Records one submitted answer exactly as the client sent it.</summary>
    public void RecordDecision(long revision, EngineDecision decision) =>
        entries.Add(new InteractionTranscriptEntry("decision", revision, Decision: decision));

    /// <summary>Records one visibility-filtered authoritative response.</summary>
    public void RecordResponse(string operation, EngineResponse response)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(response);

        IReadOnlyList<EventPresentation>? narrative = response.World is null
            ? response.Events.Count == 0 ? [] : null
            : EventCuePlanner.Plan(response.Events, response.World, previousOutcome).History;
        if (response.World is not null)
        {
            previousOutcome = response.World.Outcome;
        }

        entries.Add(new InteractionTranscriptEntry(
            operation,
            response.Revision,
            Response: new InteractionTranscriptResponse(
                response.Version,
                response.Prompt,
                response.Events,
                narrative,
                response.World,
                response.Error,
                response.Revision,
                response.History)));
    }

    /// <summary>Serializes one canonical report suitable for copying or saving.</summary>
    public string Export() => JsonSerializer.Serialize(
        new InteractionTranscriptReport(
            Format, Schema, Seed, Runtime, Setup, entries, Limitations: []), Options) + "\n";

    /// <summary>Reads a current or legacy interaction report.</summary>
    public static InteractionTranscriptReport Read(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        string? format = root.TryGetProperty("format", out JsonElement formatElement)
            ? formatElement.GetString()
            : null;
        int schema = root.TryGetProperty("schema", out JsonElement schemaElement)
            ? schemaElement.GetInt32()
            : 0;
        if (!string.Equals(format, Format, StringComparison.Ordinal))
        {
            throw new JsonException("The document is not a Marvel interaction report.");
        }

        return schema switch
        {
            Schema => JsonSerializer.Deserialize<InteractionTranscriptReport>(json, Options)
                ?? throw new JsonException("The interaction report is empty."),
            1 => UpgradeLegacy(JsonSerializer.Deserialize<LegacyInteractionTranscriptReport>(
                    json, Options)
                ?? throw new JsonException("The interaction report is empty.")),
            _ => throw new JsonException($"Interaction report schema {schema} is unsupported."),
        };
    }

    private static InteractionTranscriptReport UpgradeLegacy(
        LegacyInteractionTranscriptReport legacy) =>
        new(
            legacy.Format,
            legacy.Schema,
            legacy.Seed,
            legacy.Runtime,
            InteractionTranscriptSetup.Unavailable("legacy_report"),
            legacy.Entries.Select(entry => new InteractionTranscriptEntry(
                entry.Kind,
                entry.Revision,
                entry.Decision,
                entry.Response is null
                    ? null
                    : new InteractionTranscriptResponse(
                        entry.Response.Version,
                        entry.Response.Prompt,
                        Events: null,
                        Narrative: null,
                        entry.Response.World,
                        entry.Response.Error,
                        entry.Response.Revision,
                        entry.Response.History))).ToArray(),
            [
                "Schema 1 did not retain event evidence or narrative presentation.",
                "Schema 1 did not retain the selected game setup.",
            ]);

    private sealed record LegacyInteractionTranscriptReport(
        string Format,
        int Schema,
        uint? Seed,
        RuntimeIdentity? Runtime,
        IReadOnlyList<LegacyInteractionTranscriptEntry> Entries);

    private sealed record LegacyInteractionTranscriptEntry(
        string Kind,
        long Revision,
        EngineDecision? Decision = null,
        EngineResponse? Response = null);
}

/// <summary>The stable envelope for a shareable client interaction report.</summary>
public sealed record InteractionTranscriptReport(
    string Format,
    int Schema,
    uint? Seed,
    RuntimeIdentity? Runtime,
    InteractionTranscriptSetup Setup,
    IReadOnlyList<InteractionTranscriptEntry> Entries,
    IReadOnlyList<string> Limitations);

/// <summary>The complete game setup known to the reporting viewer.</summary>
public sealed record InteractionTranscriptSetup(
    string Availability,
    string? Scenario,
    string? Mode,
    IReadOnlyList<string>? Seats,
    string? ModularSelection,
    IReadOnlyList<string>? ModularSets)
{
    /// <summary>Builds normalized setup evidence from a successfully opened selection.</summary>
    public static InteractionTranscriptSetup FromSelection(
        SetupChoices choices,
        GameSetupSelection selection)
    {
        ArgumentNullException.ThrowIfNull(choices);
        ArgumentNullException.ThrowIfNull(selection);
        ScenarioSetupChoice scenario = choices.Scenarios.Single(
            candidate => candidate.Key == selection.ScenarioKey);
        IReadOnlyList<string> modularSets = selection.Modular switch
        {
            ModularConfiguration.Recommended => scenario.RecommendedModularSets,
            ModularConfiguration.None => [],
            ModularConfiguration.Selected => choices.ModularSets
                .Where(candidate => selection.ModularKeys.Contains(candidate.Key))
                .Select(candidate => candidate.Key)
                .ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(selection)),
        };
        return new InteractionTranscriptSetup(
            "known",
            scenario.Key,
            scenario.Expert ? "expert" : "standard",
            selection.HeroKeys.ToArray(),
            selection.Modular.ToString().ToLowerInvariant(),
            modularSets.ToArray());
    }

    /// <summary>Marks setup facts that this viewer never received.</summary>
    public static InteractionTranscriptSetup Unavailable(string reason) =>
        new(reason, null, null, null, null, null);
}

/// <summary>A credential-free response preserved independently of the live wire record.</summary>
public sealed record InteractionTranscriptResponse(
    int Version,
    Prompt? Prompt,
    IReadOnlyList<GameEvent>? Events,
    IReadOnlyList<EventPresentation>? Narrative,
    WorldDescriptor? World,
    EngineError? Error,
    long Revision,
    HistoryDescriptor? History);

/// <summary>One submitted decision or authorized engine response.</summary>
public sealed record InteractionTranscriptEntry(
    string Kind,
    long Revision,
    EngineDecision? Decision = null,
    InteractionTranscriptResponse? Response = null);
