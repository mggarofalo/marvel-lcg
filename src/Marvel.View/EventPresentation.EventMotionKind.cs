using System.Globalization;
using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>One visibility-safe, human-readable entry in the game chronology.</summary>

/// <summary>The restrained visual treatment a client may apply to an event.</summary>
public enum EventMotionKind
{
    /// <summary>A card entered the visible table.</summary>
    Create,

    /// <summary>A card or area changed visible position.</summary>
    Move,

    /// <summary>A card changed which physical face is visible.</summary>
    Flip,

    /// <summary>A visible value changed in the harmful direction.</summary>
    Damage,

    /// <summary>A visible value changed in the restorative direction.</summary>
    Heal,

    /// <summary>A named card was defeated and left play.</summary>
    Defeat,

    /// <summary>A named counter changed.</summary>
    Counter,

    /// <summary>A status card entered or left its area.</summary>
    Status,

    /// <summary>Another visible field changed.</summary>
    State,

    /// <summary>The game reached its final outcome.</summary>
    Terminal,
}
