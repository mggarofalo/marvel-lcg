using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>

/// <summary>One independent component of a durable simultaneous payment.</summary>
public sealed record ResourceCostComponentRecord(
    [property: JsonRequired] string Cost,
    [property: JsonRequired] IReadOnlyList<string>? Rule,
    [property: JsonRequired] bool Printed)
{
    /// <summary>Captures one engine-authored payment component.</summary>
    public static ResourceCostComponentRecord From(ResourceCost component)
    {
        ArgumentNullException.ThrowIfNull(component);
        return new(component.Cost, component.Rule, component.Printed);
    }
}
