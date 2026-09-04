using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Decisions;
using Marvel.Server;

namespace Marvel.Client;

/// <summary>Collects the game information already authorized for one client.</summary>
public sealed class InteractionTranscript
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };
    private readonly List<InteractionTranscriptEntry> entries = [];

    /// <summary>The complete ordered interaction history collected by this client.</summary>
    public IReadOnlyList<InteractionTranscriptEntry> Entries => entries;

    /// <summary>The deterministic seed, when this client opened the game.</summary>
    public uint? Seed { get; private set; }

    /// <summary>The runtime identity discovered from the engine.</summary>
    public RuntimeIdentity? Runtime { get; private set; }

    /// <summary>Starts a fresh transcript without retaining a prior table.</summary>
    public void Reset(uint? seed, RuntimeIdentity? runtime)
    {
        Seed = seed;
        Runtime = runtime;
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
        entries.Add(new InteractionTranscriptEntry(
            operation,
            response.Revision,
            Response: response with
            {
                RequestId = string.Empty,
                GameId = string.Empty,
                Capability = null,
                Invitations = null,
            }));
    }

    /// <summary>Serializes one canonical report suitable for copying or saving.</summary>
    public string Export() => JsonSerializer.Serialize(
        new InteractionTranscriptReport(
            "marvel-client-interaction", 1, Seed, Runtime, entries), Options) + "\n";
}

/// <summary>The stable envelope for a shareable client interaction report.</summary>
public sealed record InteractionTranscriptReport(
    string Format,
    int Schema,
    uint? Seed,
    RuntimeIdentity? Runtime,
    IReadOnlyList<InteractionTranscriptEntry> Entries);

/// <summary>One submitted decision or authorized engine response.</summary>
public sealed record InteractionTranscriptEntry(
    string Kind,
    long Revision,
    EngineDecision? Decision = null,
    EngineResponse? Response = null);
