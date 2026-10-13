namespace Marvel.Rules.Events;

/// <summary>Explicit player-phase procedure boundaries, chosen by the engine.</summary>
public enum ActivityPhaseStep
{
    /// <summary>One seat's selected hand discard.</summary>
    DiscardToHandSize,
    /// <summary>The table's simultaneous draws.</summary>
    DrawToHandSize,
    /// <summary>The table's simultaneous ready step.</summary>
    ReadyCards,
}
