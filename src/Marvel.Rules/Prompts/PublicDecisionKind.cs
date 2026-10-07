namespace Marvel.Rules.Prompts;

/// <summary>Public purpose of a pending decision, without its choices or private meaning.</summary>
/// <remarks>The engine chooses these passive wire categories; they grant no authority.</remarks>
public enum PublicDecisionKind
{
    /// <summary>A choice whose detailed options remain private.</summary>
    Choice,
    /// <summary>The active player's action menu.</summary>
    PlayerAction,
    /// <summary>Opening-hand replacement choices.</summary>
    OpeningHand,
    /// <summary>Hand discards before drawing and readying.</summary>
    EndPhaseDiscards,
    /// <summary>An interrupt opportunity around the public cause.</summary>
    Interrupt,
    /// <summary>A response opportunity after the public cause.</summary>
    Response,
    /// <summary>An ability opportunity.</summary>
    Ability,
    /// <summary>Defense against an incoming attack.</summary>
    Defense,
    /// <summary>An ordering decision.</summary>
    Order,
    /// <summary>The engaged player's minion activation order.</summary>
    MinionActivationOrder,
    /// <summary>A committed search selecting among its authorized matching cards.</summary>
    CardSearch,
    /// <summary>A choice among cards an ability allows its player to look at.</summary>
    CardLook,
}
