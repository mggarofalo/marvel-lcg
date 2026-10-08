using Marvel.Rules.Timing;

namespace Marvel.Rules.State;

/// <summary>One evaluated operation and its resulting quantity.</summary>
/// <remarks>A null source carries no claim about the identity that created it.</remarks>
public sealed record CardValueStep(
    CardValueStepKind Kind,
    long Amount,
    long Result,
    CardSourceSnapshot? Source = null,
    Duration? Duration = null);
