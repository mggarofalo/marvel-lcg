using Marvel.Rules.Events;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Resolves turn-beginning windows before exposing ordinary player actions.</summary>
internal static class GameTurnBeginning
{
    internal static Resolution BeginTurn(this Game game, List<GameEvent> events)
    {
        game.world.Agenda.PlayerTurnOpen = true;
        // The engine omits an unobserved empty occurrence to preserve prior-content replay identity.
        // Presence is declarative: costs, optional choices and current trigger legality are not guessed here.
        if (game.world.WindowAbilities.ObservesPlayerTurnBeginning(game.world))
            game.world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.BeginPlayerTurn),
                game.Round, 0, Seat: game.Active)
            { OccurrenceId = game.world.Agenda.nextPlayerActionOccurrence-- });
        return game.Turn(events);
    }
}
