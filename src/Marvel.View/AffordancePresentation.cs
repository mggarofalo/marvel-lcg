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
    /// <summary>Readable engine-authored choice name, without changing command identity.</summary>
    public string? DisplayLabel { get; init; }

    /// <summary>Authorized engine-authored commitment text for this exact choice.</summary>
    public string? CommitLabel { get; init; }

    /// <summary>Authorized source name, distinguishing identical readable copies in their area.</summary>
    public string? SourceName { get; init; }

    /// <summary>Current authorized source HP, readiness and counters, without a predicted outcome.</summary>
    public string? SourceState { get; init; }

    /// <summary>Engine-authored non-resource costs of accepting this offered ability.</summary>
    public string? CostDescription { get; init; }

    /// <summary>The declared namespace for <see cref="AnchorId"/>.</summary>
    public AffordanceAnchorKind AnchorKind { get; init; } = AffordanceAnchorKind.Unspecified;

    /// <summary>Structured source information; null is the complete fallback path.</summary>
    public AffordanceSourceDescriptor? Source { get; init; }

    /// <summary>The offered target structure without flattening groups, order, or repetition.</summary>
    public TargetRequest? TargetRequest { get; init; }

    /// <summary>The engine identifies accepting this offer as playing its source card.</summary>
    public bool PlaysCard { get; init; }

    /// <summary>The engine establishes a separate target choice after costs, if the effect reaches resolution.</summary>
    public bool DeferredTargetSelection { get; init; }

    /// <summary>The offered costs without flattening alternatives, components, variables, or generators.</summary>
    public IReadOnlyList<CostOption> CostOptions { get; init; } = [];

    /// <summary>Visible source-to-target and source-to-generator relationships in wire order.</summary>
    public IReadOnlyList<TableRelationshipDescriptor> Relationships { get; init; } = [];

    /// <summary>The explicit card anchor when it is safe to bind the affordance to that card.</summary>
    public int? CardAnchorId => Source?.CardId ?? (AnchorKind == AffordanceAnchorKind.Card
        ? AnchorId
        : null);
}
