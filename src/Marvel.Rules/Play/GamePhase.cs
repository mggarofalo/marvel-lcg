using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Where a game is in the round structure.</summary>
/// <remarks>
/// The order is <c>rr:game-round</c>'s: a round is a player phase and then a
/// villain phase, and the player phase is each player's turn in player order,
/// each turn ending with that player's end phase.
/// </remarks>
public enum GamePhase
{
    /// <summary>Before round one. Each player may mulligan their opening hand.</summary>
    Mulligan,

    /// <summary>Player-card Setup abilities resolve before round one.</summary>
    PlayerSetup,

    /// <summary>A player is taking their turn.</summary>
    PlayerTurn,

    /// <summary>That player's turn has ended and they are resolving their end phase.</summary>
    EndPhase,

    /// <summary>Every player has finished. The villain acts.</summary>
    VillainPhase,

    /// <summary>Somebody has won.</summary>
    Over,
}
