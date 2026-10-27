namespace Marvel.Rules.Play;

/// <summary>Closed stages of one rules procedure.</summary>
public enum PhaseTransitionKind
{
    /// <summary>PassFirstPlayerToken procedure stage.</summary>
    PassFirstPlayerToken,
    /// <summary>EndVillainPhase procedure stage.</summary>
    EndVillainPhase,
    /// <summary>DrawToHandSize procedure stage.</summary>
    DrawToHandSize,
    /// <summary>ReadyCards procedure stage.</summary>
    ReadyCards,
    /// <summary>EndPlayerPhase procedure stage.</summary>
    EndPlayerPhase,
    /// <summary>EndPlayerTurn procedure stage.</summary>
    EndPlayerTurn,
    /// <summary>Begin a participating player's turn.</summary>
    BeginPlayerTurn,
    /// <summary>Complete villain step one after its threat assignment and nested windows.</summary>
    EndVillainStepOne,
    /// <summary>Begin the villain phase before its first threat placement.</summary>
    BeginVillainPhase,
}
