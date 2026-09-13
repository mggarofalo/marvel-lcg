using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Client;

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
