using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>Replay identities selected by this engine build and its datasets.</summary>

/// <summary>One frozen schema 2 history unit.</summary>
internal sealed record SchemaTwoJournalUnit(
    [property: JsonRequired] string Role,
    [property: JsonRequired] string Status,
    [property: JsonRequired] int InitiatingSeat,
    [property: JsonRequired] int ActiveSeat,
    [property: JsonRequired] int Round,
    [property: JsonRequired] string Phase,
    [property: JsonRequired] IReadOnlyList<SchemaTwoJournalStep> Decisions,
    [property: JsonRequired] IReadOnlyList<InformationExposure> Exposures);
