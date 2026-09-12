using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>
/// How the game ended, or that it has not.
/// </summary>
/// <remarks>
/// The rules name two endings and they are not the same fact.
/// <c>rr:main-scheme-main-scheme-deck.2.1</c>: "if the villain completes the
/// final stage of the main scheme deck, <b>the villain wins the game</b>."
/// <c>rr:villain-defeat</c>: "if the final stage of the villain deck is
/// defeated, <b>the players win the game</b>." A boolean can say a game is over
/// and cannot say which of those happened.
/// </remarks>
public enum Outcome
{
    /// <summary>The game is still being played.</summary>
    Unfinished = 0,

    /// <summary>The players defeated the final villain stage.</summary>
    PlayersWin,

    /// <summary>The villain completed the final main scheme.</summary>
    VillainWins,

    /// <summary>
    /// The encounter deck and its discard pile emptied together.
    /// </summary>
    /// <remarks>
    /// <c>rr:encounter-deck.4</c>, and it is worded from the players' side
    /// rather than the villain's: "an infinite loop occurs with an infinite
    /// number of acceleration tokens being placed next to the main scheme deck.
    /// <b>If this happens, the players lose.</b>" Kept apart from
    /// <see cref="VillainWins"/> because the cause is different and a player
    /// asking why they lost deserves the difference.
    /// </remarks>
    PlayersLose,
}
