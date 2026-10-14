using Marvel.Rules.Play;

namespace Marvel.Godot;

/// <summary>Names only the supplied terminal outcome, without deriving a cause from the board.</summary>
internal sealed record OutcomePresentation(string Heading, string Result, string Detail)
{
    internal static OutcomePresentation From(Outcome outcome) => outcome switch
    {
        Outcome.PlayersWin => new("Victory", "The players won.", CauseUnavailable),
        Outcome.VillainWins => new("Defeat", "The villain won.", CauseUnavailable),
        Outcome.PlayersLose => new("Defeat", "The players lost.", CauseUnavailable),
        Outcome.Unfinished => new("Waiting for another player", "The game is still in progress.", ""),
        _ => new("Game complete", "The game ended.", CauseUnavailable),
    };

    private const string CauseUnavailable = "A detailed cause is not supplied for this result.";
}
