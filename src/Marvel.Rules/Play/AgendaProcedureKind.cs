using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>The phase-neutral procedure that owns an agenda operation.</summary>
public enum AgendaProcedureKind
{
    /// <summary>Enemy and player attack procedures.</summary>
    Attack,
    /// <summary>Threat placement and enemy scheme procedures.</summary>
    Threat,
    /// <summary>Encounter-card reveal procedures.</summary>
    Reveal,
    /// <summary>Character and scheme defeat procedures.</summary>
    Defeat,
    /// <summary>Accepted player actions and limit choices.</summary>
    PlayerAction,
    /// <summary>Card-ability continuation procedures.</summary>
    AbilityContinuation,
    /// <summary>Villain-phase enemy activation planning.</summary>
    Activation,
    /// <summary>Player and villain phase transitions.</summary>
    PhaseTransition,
    /// <summary>Lifecycle occurrences whose mutation already happened.</summary>
    Lifecycle,
}
