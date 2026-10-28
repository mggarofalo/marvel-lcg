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
    /// have.
    /// </exception>
    /// <exception cref="ArgumentException">The answer is rejected before the world is touched.</exception>
    /// <exception cref="InvalidOperationException">The game is already over.</exception>
    public static Resolution Resolve(this Game game, Decision input)
    {
        int round = game.Round;
        Resolution result = ResolveInput(game, input);
        return result with { Events = result.Events.Select(happened => happened.Round is null
            ? happened with { Round = round } : happened).ToArray() };
    }

    private static Resolution ResolveInput(Game game, Decision input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (game.Pending is null)
        {
            throw new InvalidOperationException("the game is over; there is nothing to answer");
        }

        var assessment = DecisionAssessment.Assess(game.Pending, input);
        if (!assessment.IsAccepted)
            throw new ArgumentException(assessment.Reason ?? assessment.Rejection.ToString(), nameof(input));
        ValidateLiveCardPlay(game, input);
        GameFormChanges.Validate(game, input);
        game.world.ClearInformationSignals();

        // Rules questions and windows retain their sequence owner across phases.
        // rr:attack-player-ability-type.step.7-.8 puts windows around powers;
        // rr:attack-enemy-activation.step.2 asks who defends an enemy attack.
        if (game.Phase is GamePhase.ScenarioSetup or GamePhase.PlayerSetup or GamePhase.PlayerTurn
            && game.asking == Asker.Sequence) return ResolveSequence(game, input);

        if (!input.IsDecline && game.Phase != GamePhase.VillainPhase)
        {
            return ResolveAffordance(game, input);
        }

        return ResolveDeclineOrVillain(game, input);
    }

    private static void ValidateLiveCardPlay(Game game, Decision input)
    {
        if (input.IsDecline || game.Phase != GamePhase.PlayerTurn || !game.IsRootPrompt) return;
        var offer = game.Pending!.Affordances.First(option => option.Id == input.Affordance);
        if (offer.Verb != CardPlay.Verb) return;

        // A published offer records a past board. The engine chooses to reject
        // an obsolete play before consuming information or committing any costs.
        if (!CardPlayLegality.Permitted(game.world, game.facts, game.world.Seats[game.Active],
                game.world.Cards[offer.AnchorId], input.Targets, game.world.CardPlayAbilities))
            throw new ArgumentException("the offered card can no longer be played", nameof(input));

        var seat = game.world.Seats[game.Active];
        var card = game.world.Cards[offer.AnchorId];
        var price = CardPayment.Price(game.world, game.facts, seat, card,
            game.world.CardPlayAbilities.AttachmentTargets(game.world, card) is null ? null : input.Targets.Single());
        if (price is null || !ResourcePayment.Allows(price, input.Spent, input.Values, input.Allocated))
            throw new ArgumentException("the offered payment no longer pays the card's current cost", nameof(input));

        try
        {
            // Preparation only reads costs, eligibility and contributors. Keep it
            // ahead of information clearing as well as exposure and cost commitment.
            _ = CardPlayPayment.Prepare(game.world, new CardPlayInitiation(
                game.facts, game.world.CardPlayAbilities, seat, card,
                CardPayment.CostOf(game.world, game.facts, seat, card, input.Targets.SingleOrDefault()), input.Targets, input.Spent));
        }
        catch (RulesNotImplementedException rejected)
        {
            // A published answer whose live payment cannot be prepared is an
            // invalid answer at this boundary; direct engine calls retain their contract.
            throw new ArgumentException(rejected.Message, nameof(input), rejected);
        }
    }

    private static Resolution ResolveDeclineOrVillain(Game game, Decision input)
    {
        switch (game.Phase)
        {
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
        if (game.Phase == GamePhase.ScenarioSetup) return game.ContinueScenarioSetup(during);
        if (game.Phase == GamePhase.PlayerSetup) return game.ContinuePlayerSetup(during);
        return game.endingPlayerPhase ? game.ContinueForcedActions(during) : game.Turn(during);
    }

    private static Resolution ResolveAffordance(Game game, Decision input)
    {
        string verb = game.Pending!.Affordances
            .FirstOrDefault(affordance => affordance.Id == input.Affordance)?.Verb
            ?? $"affordance {input.Affordance}";
        if (verb == ChangeForm) return game.ChangeFormNow(input);
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
