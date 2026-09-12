using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>

/// <summary>One durable answer together with the derived facts replay verifies.</summary>
public sealed record JournalStep(
    [property: JsonRequired] PromptRecord Prompt,
    [property: JsonRequired] DurableDecision Decision,
    [property: JsonRequired] IReadOnlyList<JsonElement> Events,
    [property: JsonRequired] long RngWords,
    [property: JsonRequired] string StateFingerprint,
    [property: JsonRequired] EngineResultRecord? Result = null)
{
    /// <summary>Captures engine output in event order after a resolved answer.</summary>
    public static JournalStep From(
        int actor,
        Prompt prompt,
        Decision decision,
        IReadOnlyList<GameEvent> events,
        long rngWords,
        string stateFingerprint,
        EngineResultRecord? result = null)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentException.ThrowIfNullOrEmpty(stateFingerprint);
        return new(
            PromptRecord.From(prompt),
            DurableDecision.From(actor, prompt, decision),
            [.. events.Select(JournalJson.Event)],
            rngWords,
            stateFingerprint,
            result);
    }
}
