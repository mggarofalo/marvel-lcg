using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

/// <summary>One generated icon explicitly assigned by the local player.</summary>

/// <summary>How the current prompt represents its target selection.</summary>
public enum TargetSelectionMode
{
    /// <summary>The affordance accepts no target selection.</summary>
    None,

    /// <summary>The player selects distinct targets within offered bounds.</summary>
    Ordinary,

    /// <summary>The player may select one target more than once.</summary>
    Repeated,

    /// <summary>The player selects one complete engine-authored group.</summary>
    Grouped,
}
