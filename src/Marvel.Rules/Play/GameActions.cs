using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Rules.Play.Game;

namespace Marvel.Rules.Play;

/// <summary>Owns actions behavior for a game.</summary>
internal static class GameActions
{

    /// <summary>
    /// Flip the active player's identity, and ask them again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:form-change-form.1</c>: "Once each round, during their turn, each
    /// player is permitted to change form by flipping their identity card." All
    /// three qualifications are load-bearing and all three are enforced here —
    /// once, each round, during their turn.
    /// </para>
    /// <para>
    /// The turn does <b>not</b> end. Changing form is one thing a player may do
    /// in their turn rather than the whole of it, so the same prompt is put
    /// again — this time without the option, because it has been used.
    /// </para>
    /// <para>
    /// <c>rr:form-change-form.3</c> is why this counter lives on the seat and
    /// not inside <see cref="Forms.Change"/>: "if a card ability causes a player
    /// to change forms, it does not count against the one voluntary form change
    /// the player is permitted". An ability calls the flip without touching the
    /// count, so the count belongs to the permission and not to the flip.
    /// </para>
    /// </remarks>
    internal static Resolution ChangeFormNow(this Game game)
    {
        var seat = game.world.Seats[game.Active];
        if (seat.FormChangedInRound == game.Round)
        {
            throw new RulesNotImplementedException(
                $"'{seat.Name}' has already changed form in round {game.Round}, and "
                + "rr:form-change-form.1 permits one voluntary change each round");
        }

        string was = Forms.ChangeAndSchedule(game.world, seat, game.facts, game.Round);
        seat.FormChangedInRound = game.Round;

        var happened = new List<GameEvent>
        {
            new CardFormChanged(seat.IdentityCard.ObjectId, was, seat.IdentityCard.FaceId)
            {
                // The moment and the action, as every other event carries them.
                // A client is told a card changed face and, without these, not
                // why -- and `rr:player-turn.1` makes changing form one of the
                // six things a turn offers rather than something that merely
                // happens.
                Trigger = TurnTrigger, Verb = ChangeForm,
            },
        };

        return game.Turn(happened);
    }

    /// <summary>
    /// Uses a basic power, or answers null if that verb is not one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:player-turn</c>: "each option, <b>except 'change form'</b>, may be
    /// performed as many times as the player is able" — so the turn does not
    /// end here. The same prompt is put again, and what it offers the second
    /// time is whatever is still possible: after a basic attack the character
    /// is exhausted, so nothing is.
    /// </para>
    /// <para>
    /// The target comes off <see cref="Decision.Targets"/> rather than being
    /// chosen here. <c>rr:initiating-abilities</c> separates choosing a target
    /// from paying for it and from resolving, and the affordance already said
    /// which targets were legal.
    /// </para>
    /// </remarks>
    /// <param name="game">The game.</param>
    /// <param name="verb">The affordance's verb.</param>
    /// <param name="input">The answer, carrying the target.</param>
    internal static Resolution? BasicPower(this Game game, string verb, Decision input)
    {
        var happened = new List<GameEvent>();

        // Which character is using the power is the affordance's anchor: the
        // identity for a hero's own, the ally for `rr:player-turn.4`. Reading
        // it here rather than switching on the verb is what keeps the two from
        // needing separate verbs -- the recording spells an ally's attack
        // `Attack`, the same as a hero's.
        var taken = game.Pending!.Affordances
            .FirstOrDefault(option => option.Id == input.Affordance);
        var user = taken is not null
            ? game.world.Cards[taken.AnchorId]
            : game.world.Seats[game.Active].IdentityCard;

        if (!ValidBasicTargets(verb, taken, input))
        {
            throw new RulesNotImplementedException(
                $"basic {verb} target selection does not satisfy affordance {taken!.Id}");
        }

        if (!UseBasicPower(game, input, verb, user, happened)) return null;

        return game.Turn(happened);
    }

    private static bool ValidBasicTargets(string verb, Affordance? taken, Decision input) =>
        verb is not (BasicPowers.AttackVerb or BasicPowers.ThwartVerb)
        || taken?.Targets is not { } requested || requested.Allows(input.Targets);

    private static bool UseBasicPower(
        Game game, Decision input, string verb, Card user, List<GameEvent> events)
    {
        if (verb is BasicPowers.AttackVerb or BasicPowers.ThwartVerb
            && input.Targets.Count == 0)
        {
            BasicPowerStatus.CancelledBasicPower(game.world, game.facts, user, verb, events);
            return true;
        }
        bool ally = game.facts.Kind(user.FaceId) == CardKind.Ally;
        if (ally && verb is BasicPowers.AttackVerb or BasicPowers.ThwartVerb)
            AllyBasicPowers.AllyPower(game.world, game.facts, user,
                game.world.Cards[Only(input, verb)], verb, events);
        else if (verb == BasicPowers.AttackVerb)
            BasicPowerInitiation.BasicAttack(game.world, game.facts, game.Active,
                game.world.Cards[Only(input, verb)], events);
        else if (verb == BasicPowers.ThwartVerb)
            BasicThwartPowers.BasicThwart(game.world, game.facts, game.Active,
                game.world.Cards[Only(input, verb)], events);
        else if (verb == BasicPowers.RecoverVerb)
            AllyBasicPowers.BasicRecovery(game.world, game.facts, game.Active, events);
        else return false;
        return true;
    }

    /// <summary>
    /// Triggers the "Action" ability an affordance is anchored to —
    /// <c>rr:player-turn.5</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Like a basic power, the turn does not end: <c>rr:player-turn</c> lets
    /// every option except changing form "be performed as many times as the
    /// player is able", so what is put again is the turn prompt with whatever
    /// is still possible.
    /// </para>
    /// <para>
    /// The ability is found again from the card rather than carried on the
    /// affordance, for the same reason a suspended choice is: an affordance is
    /// a small value on the wire and an ability is a tree.
    /// </para>
    /// </remarks>
    internal static Resolution TriggerAction(this Game game, Decision input)
    {
        var taken = game.Pending!.Affordances.First(option => option.Id == input.Affordance);
        var ability = game.world.ActionAbilities.Actions(game.world, taken.AnchorPlayer)
            .FirstOrDefault(pending => pending.Card == taken.AnchorId
                && pending.Player == taken.AnchorPlayer
                && game.Handle(
                    $"{ActionVerb}:{pending.Type}:{pending.Ordinal}:player:{pending.Player}",
                    pending.Card) == taken.Id);

        if (ability.Card != taken.AnchorId)
        {
            throw new RulesNotImplementedException(
                $"card {taken.AnchorId} has no action this player can trigger");
        }

        // `rr:initiating-abilities.step.5` -- the answer carries which cards
        // were spent, because a cost of resources is a choice of *which* and
        // the affordance already said what could pay. Copies make the agenda
        // value independent of the caller's mutable decision lists.
        game.world.Agenda.AddPlayerAction(game.Round, new PlayerAction(
            ability, [.. input.Spent], [.. input.Targets],
            new Dictionary<string, long>(input.DefinedValues, StringComparer.Ordinal),
            [.. input.Allocated]));
        if (ability.Type == AbilityType.ForcedAction)
        {
            // Resolving the ability once satisfies rr:action.2 for this player
            // phase even when the printed ability remains legal afterwards.
            // Printed use limits are a separate card-ability rule.
            game.satisfiedForcedActions.Add(
                (ability.Card, game.world.Cards[ability.Card].Incarnation,
                    game.world.Cards[ability.Card].FaceId, ability.Type, ability.Ordinal));
        }

        return game.endingPlayerPhase ? game.ContinueForcedActions([]) : game.Turn([]);
    }

    /// <summary>
    /// Plays the card an affordance is anchored to.
    /// </summary>
    /// <remarks>
    /// The card is the affordance's anchor and the payment is
    /// <see cref="Decision.Spent"/> — <c>rr:initiating-abilities</c> keeps
    /// choosing a card, determining its cost and paying that cost in separate
    /// steps, and the answer carries the last of them. Like a basic power, the
    /// turn does not end.
    /// </remarks>
    /// <param name="game">The game.</param>
    /// <param name="input">The answer, carrying the resources spent.</param>
    internal static Resolution PlayCard(this Game game, Decision input)
    {
        var seat = game.world.Seats[game.Active];
        var affordance = game.Pending!.Affordances.First(
            option => option.Id == input.Affordance);

        var happened = new List<GameEvent>();
        CardPlay.Play(
            game.world, game.facts, game.world.CardPlayAbilities, seat, game.world.Cards[affordance.AnchorId],
            input.Spent, happened, input.Targets);

        return game.Turn(happened);
    }

    /// <summary>The one target a basic power takes.</summary>
    internal static int Only(Decision input, string verb) =>
        input.Targets.Count == 1
            ? input.Targets[0]
            : throw new RulesNotImplementedException(
                $"a basic {verb} takes exactly one target and was given "
                + $"{input.Targets.Count}");

    /// <summary>
    /// The seat after this one in player order, or null at the end of the table.
    /// </summary>
    /// <remarks>
    /// <c>rr:in-player-order.2</c>: "the phrase 'next player' always refers to
    /// the next <i>(clockwise)</i> player in player order." Null rather than
    /// wrapping, because both callers want to know when the round of
    /// opportunities is <i>complete</i> — <c>rr:in-player-order.1</c>'s
    /// condition for stopping.
    /// </remarks>
    /// <param name="game">The game.</param>
    /// <param name="seat">The seat that has just finished.</param>
    internal static int? Next(this Game game, int seat)
    {
        int taken = ((seat - game.world.FirstPlayer) + game.world.Players) % game.world.Players;
        for (int offset = taken + 1; offset < game.world.Players; offset++)
        {
            int player = (game.world.FirstPlayer + offset) % game.world.Players;
            if (!game.world.Seats[player].Eliminated)
            {
                return player;
            }
        }

        return null;
    }
}
