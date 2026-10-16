using Marvel.Rules.Events;

namespace Marvel.Rules.Play;

/// <summary>Publishes setup decisions through the normal game answer lifecycle.</summary>
internal static class GameScenarioSetup
{
    internal static Resolution ContinueScenarioSetup(this Game game, List<GameEvent> events)
    {
        game.world.ScenarioSetup!.Advance(game.world, events);
        if (game.world.ScenarioSetup?.Pending is { } asked)
        {
            game.Pending = asked;
            game.Active = asked.Player;
            game.asking = Game.Asker.Sequence;
        }
        else
        {
            game.Phase = GamePhase.Mulligan;
            game.Active = game.world.FirstPlayer;
            game.Pending = game.MulliganPrompt();
            game.asking = Game.Asker.Game;
        }
        return new Resolution(game.world, game.Pending, events);
    }
}
