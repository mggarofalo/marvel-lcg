using Marvel.Rules.Events;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Completes a player's turn only after its ending windows and nested work resolve.</summary>
internal static class GameTurnEnd
{
    internal static Resolution FinishTurn(this Game game, List<GameEvent> events)
    {
        // rr:player-elimination.step.5: "the remaining players continue to play the game."
        // Their lasting effects and ending abilities still observe this turn's ending.
        game.endingTurn = true;
        game.world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndPlayerTurn),
            game.Round, 1, Seat: game.Active)
        { OccurrenceId = game.world.Agenda.nextPlayerActionOccurrence-- });
        return game.ContinueTurnEnd(events);
    }

    internal static Resolution ContinueTurnEnd(this Game game, List<GameEvent> events)
    {
        if (Sequence.WorkWithWorldAbilities(game.world, game.facts, events) is { } prompt)
        {
            game.Pending = prompt;
            game.asking = Game.Asker.Sequence;
            return new(game.world, prompt, events);
        }
        game.endingTurn = false;
        return game.world.IsOver ? game.Turn(events) : game.AdvanceTurn(events);
    }
}
