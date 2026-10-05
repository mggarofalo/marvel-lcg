using Marvel.Rules.Prompts;

namespace Marvel.Rules.Play;

/// <summary>Owns the authorized action menu for one seat at the current decision.</summary>
internal static class GameSeatPrompts
{
    internal static Prompt? For(Game game, int player)
    {
        if (player < 0 || player >= game.world.Players)
            throw new ArgumentOutOfRangeException(nameof(player));
        if (game.Pending is not { } pending || game.world.Seats[player].Eliminated)
            return null;
        if (!IsRootTurn(game))
            return pending.Player == player ? pending : null;

        var options = pending.Affordances.Where(option => OfferedTo(option, pending, player)).ToList();
        if (options.Count == 0 && pending.Player != player) return null;
        return ForSeat(game, pending, player, options);
    }

    private static Prompt ForSeat(Game game, Prompt pending, int player, List<Affordance> options) =>
        pending with
        {
            Player = player,
            Label = Label(game, pending, player),
            DisplayQuestion = pending.Player == player ? pending.DisplayQuestion : "Use an offered Action",
            Description = pending.Player == player ? pending.Description
                : $"{game.world.Seats[pending.Player].Name}'s turn · Round {game.Round}. "
                    + $"{game.world.Seats[player].Name} may use the offered Actions. "
                    + "Using one does not end the active player's turn. You may leave this menu open while they act.",
            // An off-turn menu invites an Action, not a sequential answer.
            // Declining it cannot end the active player's turn.
            Cancellable = pending.Player == player && pending.Cancellable,
            Affordances = options,
        };

    private static bool IsRootTurn(Game game) => game.Phase == GamePhase.PlayerTurn
        && game.asking == Game.Asker.Game && !game.endingPlayerPhase;

    private static bool OfferedTo(Affordance option, Prompt pending, int player) =>
        string.Equals(option.Verb, Game.ActionVerb, StringComparison.Ordinal)
            ? option.AnchorPlayer == player : pending.Player == player;

    private static string Label(Game game, Prompt pending, int player) => pending.Player == player
        ? pending.Label
        : $"{game.world.Seats[player].Name} may act during "
            + $"{game.world.Seats[pending.Player].Name}'s turn";
}
