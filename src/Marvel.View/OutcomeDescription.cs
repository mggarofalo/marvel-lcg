using Marvel.Rules.Play;

namespace Marvel.View;

/// <summary>Describes the supplied terminal result without inferring a cause from visible cards.</summary>
public static class OutcomeDescription
{
    /// <summary>Uses existing distinct outcomes and the rule-owned reason for a player loss.</summary>
    public static string Result(Outcome outcome, PlayerLossReason reason = PlayerLossReason.Unspecified) => outcome switch
    {
        Outcome.PlayersWin => "The final villain stage was defeated.",
        Outcome.VillainWins => "The final main scheme was completed.",
        Outcome.PlayersLose => reason switch
        {
            PlayerLossReason.AllPlayersEliminated => "All players were eliminated.",
            PlayerLossReason.EncounterCardsExhausted => "The encounter deck and discard pile are both empty.",
            _ => "The players lost.",
        },
        Outcome.Unfinished => "The game is still in progress.",
        _ => "The game ended.",
    };
}
