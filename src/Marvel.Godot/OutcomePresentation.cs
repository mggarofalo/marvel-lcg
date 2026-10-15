using Marvel.Rules.Play;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Names only the supplied terminal outcome, without deriving a cause from the board.</summary>
internal sealed record OutcomePresentation(string Heading, string Result, string Detail)
{
    internal static OutcomePresentation From(Outcome outcome,
        PlayerLossReason lossReason = PlayerLossReason.Unspecified) => outcome switch
    {
        Outcome.PlayersWin => new("Victory", OutcomeDescription.Result(outcome, lossReason), ""),
        Outcome.VillainWins or Outcome.PlayersLose => new("Defeat", OutcomeDescription.Result(outcome, lossReason), ""),
        Outcome.Unfinished => new("Waiting for another player", "The game is still in progress.", ""),
        _ => new("Game complete", "The game ended.", ""),
    };

}
