using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>Replay identities selected by this engine build and its datasets.</summary>

/// <summary>
/// Engine-authored facts needed to describe one active, completed history unit.
/// This is neither save data nor a client wire type.
/// </summary>
public sealed record HistoryUnitInspection(
    int Cursor,
    int Actor,
    string ActorName,
    string Role,
    string Phase,
    string? Verb,
    string Action,
    int? Subject,
    IReadOnlyList<int> ResourceGeneratorIds,
    IReadOnlyList<string> ResourceGenerators,
    IReadOnlyList<GameEvent> Events,
    string? Outcome);
