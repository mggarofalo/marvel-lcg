namespace Marvel.View;

/// <summary>A current quantity with its base and authorized source explanations.</summary>
/// <remarks>
/// Calculation contains only disclosed contributions, never placeholder rows or
/// cumulative values that could expose undisclosed sources. It is an inspection
/// aid, not an instruction to reconstruct CurrentValue by summing visible rows.
/// </remarks>
public sealed record CardEffectiveValue(
    long BaseValue,
    long CurrentValue,
    string BaseKind,
    bool IsModified,
    IReadOnlyList<CardValueCalculation> Calculation);
