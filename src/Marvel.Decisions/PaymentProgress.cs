using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

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
    /// <summary>Additional generic resources needed, or null when a simple count is insufficient.</summary>
    public int? RemainingRequired { get; init; }

    /// <summary>Whether selected generators could cover the cost with a legal allocation.</summary>
    public bool CanCoverCost { get; init; }

    /// <summary>Generated icons that a complete payment will lose as excess.</summary>
    public int ExcessIcons => IsSatisfied
        ? Math.Max(0, GeneratedIcons - AssignedIcons)
        : 0;
}
