using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using static Marvel.Rules.Play.Game;

namespace Marvel.Rules.Play;

/// <summary>Owns ordered discards and the simultaneous phase-end agenda.</summary>
internal static class GamePhaseEnd
{
    /// <summary>
    /// Steps 1 to 5 of <c>rr:end-of-player-phase</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Step 1 is this prompt: "in player order, each player may discard any
    /// number of cards from their hand". <b>In player order</b>, so it is asked
    /// once per nonempty hand and the phase does not move on until the last
    /// selection has resolved. Steps 2 and 3 are "simultaneously", so they are one step each
    /// on the agenda rather than one per player.
    /// </para>
    /// <para>
    /// Declining discards nothing, which the rule allows — "<b>may</b> discard
    /// any number" — right up until the hand is over its size, and
    /// <see cref="PhaseEnd.DiscardToHandSize"/> is where that is refused.
    /// </para>
    /// </remarks>
    /// <param name="game">The game.</param>
    /// <param name="input">The answer: which cards this player discards.</param>
    internal static Resolution EndOfPlayerPhase(this Game game, Decision input)
    {
        var happened = new List<GameEvent>();
        PhaseEnd.DiscardToHandSize(game.world, game.facts, game.Active, input.Targets, happened);
        return ContinueDiscards(game, game.Next(game.Active), happened);
    }

    internal static Resolution BeginPhaseDiscards(this Game game, List<GameEvent> happened) =>
        ContinueDiscards(game, game.Active, happened);

    private static Resolution ContinueDiscards(Game game, int? player, List<GameEvent> happened)
    {
        while (player is { } current)
        {
            game.Active = current;
            // rr:end-of-player-phase.step.1: "each player may discard any
            // number of cards from their hand". With none, no selection exists.
            if (game.world.Seats[current].Hand.Cards.Count > 0)
            {
                game.Pending = game.EndPhasePrompt();
                game.asking = Asker.Game;
                return new Resolution(game.world, game.Pending, happened);
            }
            player = game.Next(current);
        }

        game.Phase = GamePhase.VillainPhase;
        game.world.Agenda.Add(new PhaseStep(Steps.DrawToHandSize, game.Round, 2));
        game.world.Agenda.Add(new PhaseStep(Steps.ReadyCards, game.Round, 3));
        game.world.Agenda.Add(new PhaseStep(Steps.EndPlayerPhase, game.Round, 4));
        VillainPhase.Schedule(game.world.Agenda, game.Round);
        return game.Work(happened);
    }

}
