namespace Marvel.Rules.State;

/// <summary>A quantity and the ordered operations that produced that same result.</summary>
public sealed record CardValueEvaluation(
    string Field,
    long BaseValue,
    CardValueBaseKind BaseKind,
    long CurrentValue,
    IReadOnlyList<CardValueStep> Steps);
