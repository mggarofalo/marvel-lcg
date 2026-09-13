using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Server;

/// <summary>One authored scenario and mode choice.</summary>
public sealed record ScenarioSetupChoice(
    string Key,
    string Name,
    bool Expert,
    IReadOnlyList<string> RecommendedModularSets);
