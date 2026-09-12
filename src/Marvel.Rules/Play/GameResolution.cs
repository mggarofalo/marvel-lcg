using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Rules.Play.Game;

namespace Marvel.Rules.Play;

/// <summary>Owns resolution behavior for a game.</summary>
public static class GameResolution
{

    /// <summary>Applies one answer and produces the next question.</summary>
    /// <param name="game">The game.</param>
    /// <param name="input">The answer. <see cref="Decision.Decline"/> takes nothing.</param>
    /// <exception cref="RulesNotImplementedException">
    /// The answer, or the phase it leads to, needs a rule this engine does not
    /// have. Thrown before the world is touched.
    /// </exception>
    /// <exception cref="InvalidOperationException">The game is already over.</exception>
    public static Resolution Resolve(this Game game, Decision input)
    {
        ArgumentNullException.ThrowIfNull(input);
        game.world.ClearInformationSignals();

        if (game.Pending is null)
        {
            throw new InvalidOperationException("the game is over; there is nothing to answer");
        }

        // A question the sequence asked during a player's own turn is answered
        // the same way one in the villain phase is.
        // `rr:attack-player-ability-type.step.7` and `.step.8` put windows
        // around a character's attack, and a turn that could offer the question
        // and not take the answer would be a turn where no card can speak.
        //
        // **And not only windows.** An activation can begin in a player's own
        // turn — Speed Demon's forced interrupt attacks back the moment it is
        // attacked — and `rr:attack-enemy-activation.step.2` is a step that
        // asks who defends rather than a window that offers an ability. That
        // answer was reaching the verb table below and being told that taking
        // 'Defense' is not implemented, which was true only in the sense that
        // it had nowhere to go: `Sequence.Answer` has handled a step's own
        // question since it was written. the original investigation.
        if (game.Phase is GamePhase.PlayerSetup or GamePhase.PlayerTurn
            && game.asking == Asker.Sequence) return ResolveSequence(game, input);

        if (!input.IsDecline && game.Phase != GamePhase.VillainPhase)
        {
            // The turn prompts offer things that have to *do* something and
            // most of them are still not written. Naming the verb rather than
            // saying "not implemented" is the difference between a one-line
            // diagnosis and a debugging session.
            //
            // Two exceptions. The villain phase offers an ability waiting in a
            // window or a character declared as a defender, and both are
            // implemented. `Change_Form` is the other, and it is below.
            return ResolveAffordance(game, input);
        }

        return ResolveDeclineOrVillain(game, input);
    }

    private static Resolution ResolveDeclineOrVillain(Game game, Decision input)
    {
        switch (game.Phase)
        {
            case GamePhase.Mulligan:
                // Declining a mulligan keeps the opening hand, so nothing moves
                // -- which is `Mulligan` with an empty list, the same way
                // declining the end-of-phase discard is.
                //
                // `game.Active` is read from the board here and not left as it was
                // set at `Begin`: the first player is whoever holds the token
                // when the phase starts, and a scenario or a card can move it
                // between the deal and the first turn. Every later round does
                // the same in `Work`.
                return game.Mulligan(input);

            case GamePhase.PlayerSetup:
                throw new RulesNotImplementedException(
                    "a player Setup ability can only be answered through its agenda question");

            case GamePhase.PlayerTurn:
                // Declining the main turn ends it. Progress in the game's terms
                // and no change to the board -- much the largest class of no-op
                // decision there is, at 187 of 320 declines in the sample this
                // was designed against.
                //
                // `rr:player-phase`: "during the player phase, **each player**
                // *(in player order)* takes one turn". So the phase is over
                // when the last of them has had theirs, not when the first has.
                return game.endingPlayerPhase ? game.ContinueForcedActions([]) : game.FinishTurn([]);

            case GamePhase.EndPhase:
                return game.EndOfPlayerPhase(input);

            case GamePhase.VillainPhase:
                // Answering a question the villain phase asked. The window
                // absorbs the answer and the agenda carries on from where it
                // stopped.
                return game.Work(game.Answer(input));

            default:
                throw new RulesNotImplementedException($"the {game.Phase} phase is not implemented");
        }
    }

    private static Resolution ResolveSequence(Game game, Decision input)
    {
        var during = new List<GameEvent>();
        Sequence.AnswerWithWorldAbilities(game.world, game.facts, game.Pending!, input, during);
        if (game.Phase == GamePhase.PlayerSetup) return game.ContinuePlayerSetup(during);
        return game.endingPlayerPhase ? game.ContinueForcedActions(during) : game.Turn(during);
    }

    private static Resolution ResolveAffordance(Game game, Decision input)
    {
        string verb = game.Pending!.Affordances
            .FirstOrDefault(affordance => affordance.Id == input.Affordance)?.Verb
            ?? $"affordance {input.Affordance}";
        if (verb == ChangeForm) return game.ChangeFormNow();
        if (game.Phase == GamePhase.Mulligan && verb == ResolveMulligans)
            return game.Mulligan(input);
        if (game.Phase == GamePhase.PlayerTurn) return ResolveTurnAffordance(game, input, verb);
        if (game.Phase == GamePhase.EndPhase && verb == EndPhaseVerb)
            return game.EndOfPlayerPhase(input);
        throw new RulesNotImplementedException(
            $"taking '{verb}' is not implemented; this resolve only declines");
    }

    private static Resolution ResolveTurnAffordance(Game game, Decision input, string verb)
    {
        if (game.BasicPower(verb, input) is { } used) return used;
        if (verb == CardPlay.Verb) return game.PlayCard(input);
        if (verb == ActionVerb) return game.TriggerAction(input);
        throw new RulesNotImplementedException(
            $"taking '{verb}' is not implemented; this resolve only declines");
    }
}
