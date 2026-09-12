using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Rules.Play.Game;

namespace Marvel.Rules.Play;

/// <summary>Owns phases behavior for a game.</summary>
internal static class GamePhases
{

    /// <summary>
    /// Steps 1 to 5 of <c>rr:end-of-player-phase</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Step 1 is this prompt: "in player order, each player may discard any
    /// number of cards from their hand". <b>In player order</b>, so it is asked
    /// once per seat and the phase does not move on until the last has
    /// answered. Steps 2 and 3 are "simultaneously", so they are one step each
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

        if (game.Next(game.Active) is { } player)
        {
            game.Active = player;
            game.Pending = game.EndPhasePrompt();
            game.asking = Asker.Game;
            return new Resolution(game.world, game.Pending, happened);
        }

        game.Phase = GamePhase.VillainPhase;
        game.world.Agenda.Add(new PhaseStep(Steps.DrawToHandSize, game.Round, 2));
        game.world.Agenda.Add(new PhaseStep(Steps.ReadyCards, game.Round, 3));
        game.world.Agenda.Add(new PhaseStep(Steps.EndPlayerPhase, game.Round, 4));
        VillainPhase.Schedule(game.world.Agenda, game.Round);
        return game.Work(happened);
    }

    /// <summary>
    /// Resolves one player's mulligan —
    /// <c>rr:appendix-ii-setup.step.15</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Each player may discard any number of cards from hand, and then draw
    /// up to their starting hand size. <i>(Do not shuffle these discarded
    /// cards back into their decks at this time.)</i>"
    /// </para>
    /// <para>
    /// <b>Discarded, not put back.</b> The parenthesis is the whole of the
    /// difference between this and a deck-bottom mulligan, and it is
    /// observable: the cards are in the discard pile, where
    /// <c>rr:player-deck.4</c> can shuffle them into a new deck later and
    /// where a card that reads a discard pile can see them.
    /// </para>
    /// <para>
    /// <b>Draw up to, not draw that many.</b> A player who discarded three
    /// draws back to their hand size rather than three cards, which is the
    /// same distinction <see cref="PhaseEnd.DrawToHandSize"/> makes.
    /// </para>
    /// <para>
    /// <b>The two readings cannot disagree here, and that is worth knowing
    /// rather than hiding.</b> Step 14 has already drawn every player up to
    /// their hand size, so the hand this is asked about is exactly that size
    /// and "up to hand size" and "as many as went" fetch the same number. A
    /// mutation that swaps one for the other survives every test, and will
    /// until something modifies hand size during setup. The rule's number is
    /// used anyway, because the rule is what this is implementing.
    /// </para>
    /// </remarks>
    /// <param name="game">The game.</param>
    /// <param name="input">The answer, carrying the cards to discard.</param>
    internal static Resolution Mulligan(this Game game, Decision input)
    {
        var happened = new List<GameEvent>();
        var seat = game.world.Seats[game.Active];

        foreach (int id in input.Targets)
        {
            var card = game.world.Cards[id];
            if (card.Area != seat.Hand)
            {
                throw new RulesNotImplementedException(
                    $"card {id} is not in {seat.Name}'s hand, so it cannot be mulliganed");
            }

            Discard.Card(game.world, card, MulliganTrigger, happened);
        }

        long limit = PhaseEnd.HandSize(game.world, seat, game.facts);
        while (seat.Hand.Cards.Count < limit)
        {
            int before = seat.Hand.Cards.Count;
            Draw.Cards(game.world, game.Active, 1, MulliganTrigger, happened);
            if (seat.Hand.Cards.Count == before)
            {
                // `rr:player-deck.4` -- a deck and a discard pile both empty.
                // No card to draw is a legal board, not a stall.
                break;
            }
        }

        if (game.Next(game.Active) is { } player)
        {
            game.Active = player;
            game.Pending = game.MulliganPrompt();
            game.asking = Asker.Game;
            return new Resolution(game.world, game.Pending, happened);
        }

        return game.BeginPlayerSetup(happened);
    }

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

    internal static Prompt MulliganPrompt(this Game game)
    {
        var seat = game.world.Seats[game.Active];
        return new Prompt(
            Player: seat.Index,
            Asking: Question.TurnOption,
            When: Timing.TimingPriority.Untimed,
            Trigger: MulliganTrigger,
            Label: $"{seat.Name} resolves mulligans",
            // `rr:appendix-ii-setup.step.15` gives a player one thing to do
            // and lets them do none of it: "each player **may** discard any
            // number of cards from hand". Taking it with an empty list and
            // declining are the same answer, so a cancel would mean the same
            // thing twice.
            Cancellable: false,
            Affordances: [game.HandChoice(seat, ResolveMulligans)]);
    }

    /// <summary>
    /// Runs out whatever the turn just put on the agenda, then asks again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A basic attack is <c>Steps.CharacterAttacks</c> and not a call, because
    /// <c>rr:attack-player-ability-type.step.7</c> puts abilities around it and
    /// one of them may ask the player something. So what follows a turn option
    /// is the agenda draining, and the turn prompt is put again only once it
    /// has.
    /// </para>
    /// <para>
    /// <c>rr:player-turn</c> is why the turn prompt comes back at all: "each
    /// option, <b>except 'change form'</b>, may be performed as many times as
    /// the player is able", so a turn is not over because one option was taken.
    /// </para>
    /// </remarks>
    internal static Resolution Turn(this Game game, List<GameEvent> happened)
    {
        if (Sequence.WorkWithWorldAbilities(game.world, game.facts, happened) is { } asked)
        {
            game.Pending = asked;
            game.asking = Asker.Sequence;
            return new Resolution(game.world, game.Pending, happened);
        }

        if (game.world.IsOver)
        {
            // `rr:villain-defeat` -- the players can win in the middle of their
            // own turn, and nothing is asked of anybody after a game is over.
            game.Phase = GamePhase.Over;
            game.Pending = null;
            game.asking = Asker.Game;
            return new Resolution(game.world, null, happened);
        }

        if (game.world.Seats[game.Active].Eliminated)
        {
            return game.FinishTurn(happened);
        }

        game.Pending = game.TurnPrompt();
        game.asking = Asker.Game;
        return new Resolution(game.world, game.Pending, happened);
    }

    internal static Resolution Work(this Game game, List<GameEvent> happened)
    {
        if (Sequence.WorkWithWorldAbilities(game.world, game.facts, happened) is { } asked)
        {
            game.Pending = asked;
            game.asking = Asker.Sequence;
            return new Resolution(game.world, game.Pending, happened);
        }

        if (game.world.IsOver)
        {
            // The only thing that makes a prompt absent. Nothing is asked of a
            // player after a game is over.
            game.Phase = GamePhase.Over;
            game.Pending = null;
            game.asking = Asker.Game;
            return new Resolution(game.world, null, happened);
        }

        game.Round++;
        game.finishedTurns.Clear();
        game.satisfiedForcedActions.Clear();
        game.endingPlayerPhase = false;
        game.Phase = GamePhase.PlayerTurn;
        game.Active = game.world.FirstPlayer;
        game.Pending = game.TurnPrompt();
        game.asking = Asker.Game;
        return new Resolution(game.world, game.Pending, happened);
    }

    /// <summary>Finish the active turn and find the next participating player.</summary>
    /// <remarks>
    /// <c>rr:player-elimination.5</c> finishes an ability after eliminating its
    /// resolving player, while <c>rr:player-elimination.step.5</c> says that
    /// player no longer participates. Remembering completed seats avoids
    /// relying on the first-player token, which elimination itself may pass.
    /// </remarks>
    internal static Resolution FinishTurn(this Game game, List<GameEvent> happened)
    {
        game.finishedTurns.Add(game.Active);
        for (int offset = 1; offset < game.world.Players; offset++)
        {
            int player = (game.Active + offset) % game.world.Players;
            if (!game.finishedTurns.Contains(player) && !game.world.Seats[player].Eliminated)
            {
                game.Active = player;
                game.Pending = game.TurnPrompt();
                game.asking = Asker.Game;
                return new Resolution(game.world, game.Pending, happened);
            }
        }

        game.Active = game.world.FirstPlayer;
        game.endingPlayerPhase = true;
        return game.ContinueForcedActions(happened);
    }

    /// <summary>Finishes only the mandatory actions deferred to the phase boundary.</summary>
    internal static Resolution ContinueForcedActions(this Game game, List<GameEvent> happened)
    {
        if (Sequence.WorkWithWorldAbilities(game.world, game.facts, happened) is { } asked)
        {
            game.Pending = asked;
            game.asking = Asker.Sequence;
            return new Resolution(game.world, game.Pending, happened);
        }

        if (game.world.IsOver)
        {
            game.Phase = GamePhase.Over;
            game.Pending = null;
            game.asking = Asker.Game;
            return new Resolution(game.world, null, happened);
        }

        game.Active = game.world.FirstPlayer;
        var forced = game.TurnActions()
            .Where(ability => ability.Type == AbilityType.ForcedAction
                && !game.satisfiedForcedActions.Contains(
                    (ability.Card, game.world.Cards[ability.Card].Incarnation,
                        game.world.Cards[ability.Card].FaceId,
                        ability.Type, ability.Ordinal)))
            .ToList();
        if (forced.Count > 0)
        {
            // `rr:action.2`: every legal Forced Action must resolve before the
            // player phase can end. The player still chooses its timing during
            // the phase; once the phase would end, declining is no longer an
            // answer. Costs and targets remain ordinary action questions.
            game.Pending = game.ForcedActionsPrompt(forced);
            game.asking = Asker.Game;
            return new Resolution(game.world, game.Pending, happened);
        }

        game.endingPlayerPhase = false;
        game.Phase = GamePhase.EndPhase;
        game.Pending = game.EndPhasePrompt();
        game.asking = Asker.Game;
        return new Resolution(game.world, game.Pending, happened);
    }

    internal static List<GameEvent> Answer(this Game game, Decision input)
    {
        var happened = new List<GameEvent>();
        if (game.Pending is { } asked)
        {
            Sequence.AnswerWithWorldAbilities(game.world, game.facts, asked, input, happened);
        }

        return happened;
    }

}
