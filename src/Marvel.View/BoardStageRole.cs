using System.Globalization;
using System.Text;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>
/// The authoritative progressive-stage role retained from the engine area's
/// zone before current and upcoming stages are combined for presentation.
/// </summary>
public enum BoardStageRole
{
    /// <summary>The card is not part of a progressive villain or main-scheme stack.</summary>
    None,

    /// <summary>The card occupies the engine's active villain or main-scheme area.</summary>
    Current,

    /// <summary>The card occupies the engine's out-of-play upcoming-stage deck.</summary>
    Upcoming,
}
