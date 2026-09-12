using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

/// <summary>Count-only progress for the current visible target request.</summary>
public sealed record TargetSelectionProgress(
    TargetSelectionMode Mode,
    int Selected,
    int Minimum,
    int Maximum,
    bool IsSatisfied);
