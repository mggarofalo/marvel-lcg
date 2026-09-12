using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

/// <summary>Whether the current affordance needs a cost choice.</summary>
public enum CostSelectionState
{
    /// <summary>No affordance has been selected.</summary>
    Unavailable,

    /// <summary>The selected affordance has no cost.</summary>
    NotRequired,

    /// <summary>The player must select one offered cost.</summary>
    Required,

    /// <summary>An offered cost has been selected.</summary>
    Selected,
}
