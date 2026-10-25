using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Protocol;

/// <summary>One authored scenario and mode choice.</summary>
public sealed record ScenarioSetupChoice(
    string Key,
    string Name,
    bool Expert,
    IReadOnlyList<string> RecommendedModularSets)
{
    /// <summary>Server-authored mode identity, independent of ordinary expert-stage progression.</summary>
    public string Mode { get; init; } = Expert ? "expert" : "standard";

    /// <summary>Whether this scenario accepts the modular-set selection.</summary>
    public bool AllowsModularSets { get; init; } = true;

    /// <summary>Ordered component versions for this complete recipe.</summary>
    public IReadOnlyList<ScenarioVariantChoice> Variants { get; init; } = [];
}
