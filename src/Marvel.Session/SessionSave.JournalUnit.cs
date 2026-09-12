using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>Replay identities selected by this engine build and its datasets.</summary>

/// <summary>A replayable group of one root operation and its dependent answers.</summary>
public sealed record JournalUnit(
    [property: JsonRequired] string Role,
    [property: JsonRequired] string Status,
    [property: JsonRequired] int InitiatingSeat,
    [property: JsonRequired] int ActiveSeat,
    [property: JsonRequired] int Round,
    [property: JsonRequired] string Phase,
    [property: JsonRequired] IReadOnlyList<JournalStep> Decisions,
    [property: JsonRequired] IReadOnlyList<InformationExposure> Exposures);
