using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Client;

/// <summary>The stable envelope for a shareable client interaction report.</summary>
public sealed record InteractionTranscriptReport(
    string Format,
    int Schema,
    uint? Seed,
    RuntimeIdentity? Runtime,
    InteractionTranscriptSetup Setup,
    IReadOnlyList<InteractionTranscriptEntry> Entries,
    IReadOnlyList<string> Limitations);
