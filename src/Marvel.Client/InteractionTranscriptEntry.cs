using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Client;

/// <summary>One submitted decision or authorized engine response.</summary>
public sealed record InteractionTranscriptEntry(
    string Kind,
    long Revision,
    EngineDecision? Decision = null,
    InteractionTranscriptResponse? Response = null,
    string? Disposition = null);
