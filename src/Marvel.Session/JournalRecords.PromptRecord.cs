using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>

/// <summary>A stable, affordance-handle-free snapshot of one engine prompt.</summary>
public sealed record PromptRecord(
    [property: JsonRequired] int Player,
    [property: JsonRequired] string Asking,
    [property: JsonRequired] string When,
    [property: JsonRequired] string Trigger,
    [property: JsonRequired] string Label,
    [property: JsonRequired] bool Cancellable,
    [property: JsonRequired] IReadOnlyList<AffordanceRecord> Affordances)
{
    /// <summary>Captures every ordered prompt field except ephemeral affordance ids.</summary>
    public static PromptRecord From(Prompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        return new(
            prompt.Player,
            prompt.Asking.ToString(),
            prompt.When.ToString(),
            prompt.Trigger,
            prompt.Label,
            prompt.Cancellable,
            [.. prompt.Affordances.Select(AffordanceRecord.From)]);
    }
}
