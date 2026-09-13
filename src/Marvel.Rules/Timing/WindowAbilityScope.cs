using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Which cards may contribute abilities while a window is worked.</summary>
public enum WindowAbilityScope
{
    /// <summary>Every otherwise eligible card.</summary>
    AllCards,

    /// <summary>
    /// Encounter cards only. During game setup, player-card abilities cannot
    /// resolve unless they are Setup abilities, which resolve as the setup
    /// step itself rather than from an interrupt or response window.
    /// </summary>
    EncounterCardsOnly,
}
