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
    string? Consequence = null)
{
    /// <summary>The declared namespace for <see cref="AnchorId"/>.</summary>
    public AffordanceAnchorKind AnchorKind { get; init; } = AffordanceAnchorKind.Unspecified;

    /// <summary>Structured source information; null is the complete fallback path.</summary>
    public AffordanceSourceDescriptor? Source { get; init; }

    /// <summary>The offered target structure without flattening groups, order, or repetition.</summary>
    public TargetRequest? TargetRequest { get; init; }

    /// <summary>The offered costs without flattening alternatives, components, variables, or generators.</summary>
    public IReadOnlyList<CostOption> CostOptions { get; init; } = [];

    /// <summary>Visible source-to-target and source-to-generator relationships in wire order.</summary>
    public IReadOnlyList<TableRelationshipDescriptor> Relationships { get; init; } = [];
}
