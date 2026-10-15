namespace Marvel.Rules.Play;

/// <summary>The rule owner distinguishes causes sharing the players-lose outcome.</summary>
public enum PlayerLossReason
{
    /// <summary>No more specific cause was supplied.</summary>
    Unspecified = 0,
    /// <summary>No player remains in the game.</summary>
    AllPlayersEliminated,
    /// <summary>The encounter deck and encounter discard pile are both empty.</summary>
    EncounterCardsExhausted,
}
