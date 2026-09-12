using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>

/// <summary>The independent values in one durable cost option.</summary>
public sealed record CostOptionRecord(
    [property: JsonRequired] int Target,
    [property: JsonRequired] string Cost,
    [property: JsonRequired] IReadOnlyList<string>? Rule,
    [property: JsonRequired] string OrCost,
    [property: JsonRequired] IReadOnlyList<string>? OrRule,
    [property: JsonRequired] IReadOnlyList<ResourceSourceRecord>? Sources,
    [property: JsonRequired] IReadOnlyList<VariableRequestRecord>? Variables,
    [property: JsonRequired] IReadOnlyList<ResourceCostComponentRecord>? Components,
    [property: JsonRequired] bool DeclarationSensitive)
{
    [JsonIgnore]
    internal bool LegacyDeclarationSensitiveRecorded { get; init; } = true;

    /// <summary>Captures source fields without computed cost aliases.</summary>
    public static CostOptionRecord From(CostOption option)
    {
        ArgumentNullException.ThrowIfNull(option);
        return new(
            option.Target,
            option.Cost,
            option.Rule,
            option.OrCost,
            option.OrRule,
            option.Sources is null
                ? null
                : [.. option.Sources.Select(ResourceSourceRecord.From)],
            option.Variables is null
                ? null
                : [.. option.Variables.Select(VariableRequestRecord.From)],
            option.Components is null
                ? null
                : [.. option.Components.Select(ResourceCostComponentRecord.From)],
            option.DeclarationSensitive);
    }
}
