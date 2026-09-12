using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

/// <summary>One generated icon explicitly assigned by the local player.</summary>

/// <summary>Count-only progress for the current offered payment.</summary>
public sealed record PaymentProgress(
    CostSelectionState CostState,
    int? SelectedCost,
    int CostOptions,
    int SelectedGenerators,
    int GeneratedIcons,
    int AssignedIcons,
    int DefinedVariables,
    int RequestedVariables,
    bool IsSatisfied)
{
    /// <summary>Generated icons that a complete payment will lose as excess.</summary>
    public int ExcessIcons => IsSatisfied
        ? Math.Max(0, GeneratedIcons - AssignedIcons)
        : 0;
}
