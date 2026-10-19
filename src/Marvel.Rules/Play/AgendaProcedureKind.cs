using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>The phase-neutral procedure that owns an agenda operation.</summary>
public enum AgendaProcedureKind
{
    /// <summary>Enemy and player attack procedures.</summary>
    Attack,
    /// <summary>Damage replacement, placement and defeat completion.</summary>
    Damage,
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
    /// <summary>Accepted resource payment and post-payment card entry.</summary>
    ResourcePayment,
    /// <summary>Played card resolution and finalization.</summary>
    CardEntry,
    /// <summary>Campaign setup and expert recovery before opening hands.</summary>
    Campaign,
}
