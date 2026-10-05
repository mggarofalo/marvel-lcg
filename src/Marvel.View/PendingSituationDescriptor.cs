using Marvel.Rules.Prompts;

namespace Marvel.View;

/// <summary>Passive public purpose and readable causal objects, never answer options.</summary>
public sealed record PendingSituationDescriptor(
    PublicDecisionKind Kind,
    IReadOnlyList<int> SourceCardIds)
{
    /// <summary>Public ability sources that caused the current activation.</summary>
    public IReadOnlyList<int> CauseCardIds { get; init; } = [];
}
