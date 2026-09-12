using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>

/// <summary>The terminal meaning that a state digest alone cannot express.</summary>
public sealed record EngineResultRecord(
    [property: JsonRequired] string Outcome,
    [property: JsonRequired] int Round);
