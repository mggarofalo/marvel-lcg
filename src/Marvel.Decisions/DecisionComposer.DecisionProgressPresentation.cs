using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

/// <summary>One generated icon explicitly assigned by the local player.</summary>

/// <summary>
/// Render-ready progress derived from the exact draft and its visible prompt.
/// No object identities are retained in this presentation record.
/// </summary>
public sealed record DecisionProgressPresentation(
    TargetSelectionProgress Targets,
    PaymentProgress Payment,
    bool IsReady,
    string? Error);
