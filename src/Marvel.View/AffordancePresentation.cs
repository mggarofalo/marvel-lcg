using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.View;

/// <summary>One visible affordance row.</summary>
public sealed record AffordancePresentation(
    int Id,
    string Label,
    string? Description,
    string Verb,
    string Anchor,
    int AnchorId,
    int AnchorPlayer,
    string? Illegal,
    string Targets,
    IReadOnlyList<string> Costs,
    string? Consequence = null);
