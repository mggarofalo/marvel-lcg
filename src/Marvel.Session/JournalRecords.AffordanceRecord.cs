using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>

/// <summary>A stable snapshot of an offered choice, excluding its live handle.</summary>
public sealed record AffordanceRecord(
    [property: JsonRequired] string Verb,
    [property: JsonRequired] int AnchorId,
    [property: JsonRequired] int AnchorPlayer,
    [property: JsonRequired] string Label,
    [property: JsonRequired] TargetRequestRecord? Targets,
    [property: JsonRequired] IReadOnlyList<CostOptionRecord> Costs,
    [property: JsonRequired] string? Illegal)
{
    /// <summary>Captures an affordance in domain order.</summary>
    public static AffordanceRecord From(Affordance affordance)
    {
        ArgumentNullException.ThrowIfNull(affordance);
        return new(
            affordance.Verb,
            affordance.AnchorId,
            affordance.AnchorPlayer,
            affordance.Label,
            affordance.Targets is null ? null : TargetRequestRecord.From(affordance.Targets),
            [.. affordance.CostOptions.Select(CostOptionRecord.From)],
            affordance.Illegal);
    }
}
