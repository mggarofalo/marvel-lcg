using Marvel.Rules.Events;
using Marvel.Rules.State;
using static Marvel.Rules.Play.Game;

namespace Marvel.Rules.Play;

/// <summary>Owns player-card setup scheduling and its decision continuations.</summary>
internal static class GamePlayerSetup
{
    /// <summary>
    /// Resolves player-card Setup abilities after every mulligan and before the
    /// first player phase — setup step 16.
    /// </summary>
    internal static Resolution BeginPlayerSetup(this Game game, List<GameEvent> happened)
    {
        game.Phase = GamePhase.PlayerSetup;

        foreach (int player in game.world.PlayerOrder)
        {
            foreach (var card in game.world.SetupAbilities.PlayerSetupCards(game.world, player)
                         .DistinctBy(card => card.ObjectId)
                         .OrderBy(card => card.ObjectId))
            {
                if (!DeckTypes.IsInPlay(card.Area.Type)
                    || card.Area.PlayArea != PlayArea.Of(player))
                {
                    throw new InvalidOperationException(
                        $"card {card.ObjectId} was returned as a Setup card for player "
                        + $"{player}, but it is not in that player's play area");
                }

                game.playerSetup.Enqueue(card);
            }
        }

        return game.ContinuePlayerSetup(happened);
    }

    /// <summary>Drains setup work until it needs an answer or round one begins.</summary>
    internal static Resolution ContinuePlayerSetup(this Game game, List<GameEvent> happened)
    {
        while (true)
        {
            if (Sequence.WorkWithWorldAbilities(game.world, game.facts, happened) is { } asked)
            {
                game.Active = asked.Player;
                game.Pending = asked;
                game.asking = Asker.Sequence;
                return new Resolution(game.world, game.Pending, happened);
            }

            if (game.playerSetup.TryDequeue(out var card))
            {
                happened.AddRange(game.world.SetupAbilities.Setup(game.world, card));
                continue;
            }

            game.Round = 1;
            game.finishedTurns.Clear();
            game.Active = game.world.FirstPlayer;
            game.Phase = GamePhase.PlayerTurn;
            game.Pending = game.TurnPrompt();
            game.asking = Asker.Game;
            return new Resolution(game.world, game.Pending, happened);
        }
    }
}
