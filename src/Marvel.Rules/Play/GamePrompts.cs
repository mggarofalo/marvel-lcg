using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Rules.Play.Game;

namespace Marvel.Rules.Play;

/// <summary>Owns prompts behavior for a game.</summary>
internal static class GamePrompts
{
    internal static Prompt TurnPrompt(this Game game)
    {
        var seat = game.world.Seats[game.Active];
        return new Prompt(
            Player: seat.Index,
            Asking: Question.TurnOption,
            When: Timing.TimingPriority.Untimed,
            Trigger: TurnTrigger,
            // The engine's console line, newline and all. Normalising it is how
            // two implementations quietly stop agreeing about a string that is
            // on the wire.
            Label: $"\n--- {seat.Name}'s Turn ({game.Round}) ---",
            Cancellable: true,
            Affordances: game.TurnOptions(seat));
    }

    /// <summary>The mandatory action gate before the player phase may end.</summary>
    internal static Prompt ForcedActionsPrompt(this Game game, List<PendingAbility> forced)
    {
        // The rule says every legal Forced Action must resolve but does not
        // order actions that several players deferred to this boundary. The
        // engine chooses player order, starting with the first player (the
        // order `TurnActions` returns), and the owning player chooses among
        // their own actions and pays their own costs.
        int player = forced[0].Player;
        var options = new List<Affordance>();
        foreach (var action in forced.Where(action => action.Player == player))
        {
            var described = game.world.ActionAbilities.Describe(game.world, action);
            options.Add(described with
            {
                Verb = ActionVerb,
                Id = game.Handle(
                    $"{ActionVerb}:{action.Type}:{action.Ordinal}:player:{action.Player}",
                    described.AnchorId),
            });
        }

        return new Prompt(
            Player: player,
            Asking: Question.TurnOption,
            When: TimingPriority.Untimed,
            Trigger: TurnTrigger,
            Label: "Your Forced Actions before the player phase ends",
            Cancellable: false,
            Affordances: options);
    }

    /// <summary>
    /// What a player may do on their turn — <c>rr:player-turn</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only what is offered <i>and</i> can be taken. An affordance that would
    /// throw when taken is worse than an absent one — the original investigation is that same
    /// defect on the action menu.
    /// </para>
    /// <para>
    /// <c>rr:player-turn</c> lists six options and all six are here: change form,
    /// playing a card, ally actions, the basic powers, triggering an action,
    /// and game.asking another player to trigger one.
    /// </para>
    /// </remarks>
    /// <param name="game">The game.</param>
    /// <param name="seat">Whose turn.</param>
    internal static List<Affordance> TurnOptions(this Game game, Seat seat)
    {
        var options = new List<Affordance>();

        // `rr:form-change-form.1` permits one voluntary change each round, so a
        // player who has used theirs is not offered it again.
        if (seat.FormChangedInRound != game.Round)
        {
            options.Add(game.Anchored(ChangeForm, seat));
        }

        // `rr:player-turn.2`: "play an ally, upgrade, support, or player side
        // scheme card from hand". Priced per card, and a card that cannot be
        // paid for is not offered -- `rr:initiating-abilities.step.3` checks
        // "the player's ability to pay them" before anything is spent.
        // By object id, which is the order the recorded prompt lists them --
        // `Play@37, 45, 46, 47` against a hand held in the order
        // `42, 45, 37, 9, 47, 46`. Measured on one board, so it is the simplest
        // reading that fits rather than a rule anything states.
        AddCardPlays(game, seat, options);

        // `rr:player-turn.4`: "use an ally card they control in play to attack
        // an enemy or thwart a scheme". `rr:ally.5` puts these outside the
        // identity -- "attacks [...] that resolve from allies in play under a
        // player's control are **not** considered to be performed by that
        // player's identity" -- so they are offered whatever form the player is
        // in, and whether the identity is exhausted does not matter.
        AddAllyPowers(game, seat, options);

        // `rr:player-turn.5`: "trigger an **Action** ability on a card in play
        // they control, an encounter card in play, [...] or an event card in
        // their hand (by playing that event)". Not a window -- an action is one
        // of the six things a turn offers, so it is asked with the others.
        //
        // `rr:player-turn.6` also permits game.asking another player to trigger any
        // action they could trigger on their own turn, and permits that player
        // to offer. The engine chooses to represent an accepted request or an
        // offer as the action itself: there is no request/accept handshake.
        // `PendingAbility.Player` and `AnchorPlayer` remain the player actually
        // acting, so their form, cards, resources, targets and limits decide
        // whether the option is legal and how it resolves.
        //
        // `.5.1` is applied where the ability is found: "if the action ability
        // is preceded by Hero or Alter-Ego, the player must be in the specified
        // form", and 728 of the 966 in the pool are.
        AddActions(game, options);

        // `rr:player-turn.3`: the hero's basic attack or thwart in hero form,
        // the alter-ego's basic recovery in alter-ego form. A character that is
        // exhausted cannot pay the cost of any of them (`rr:exhausted.2`).
        if (!seat.IdentityCard.Ready)
        {
            return options;
        }

        AddIdentityPower(game, seat, options);

        return options;
    }

    private static void AddCardPlays(Game game, Seat seat, List<Affordance> options)
    {
        foreach (Card card in seat.Hand.Cards.OrderBy(card => card.ObjectId))
        {
            if (CardPayment.Price(game.world, game.facts, seat, card) is not { } price) continue;
            var hosts = CardPlayLegality.LegalAttachmentTargets(
                game.world, game.facts, seat, card, game.world.CardPlayAbilities);
            if (hosts is not { Count: 0 }) options.Add(game.Priced(seat, card, price, hosts));
        }
    }

    private static void AddAllyPowers(Game game, Seat seat, List<Affordance> options)
    {
        foreach (Card ally in AllyBasicPowers.Allies(game.world, seat.Index))
        {
            if (BasicPowers.CanUsePower(game.facts, ally, "ATK"))
                game.Offer(options, ally, BasicPowers.AttackVerb,
                    BasicPowers.Attackable(game.world, game.facts, seat.Index));
            if (BasicPowers.CanUsePower(game.facts, ally, "THW"))
                game.Offer(options, ally, BasicPowers.ThwartVerb,
                    BasicPowers.Thwartable(game.world, game.facts, seat.Index));
        }
    }

    private static void AddActions(Game game, List<Affordance> options)
    {
        foreach (PendingAbility action in game.TurnActions())
        {
            Affordance described = game.world.ActionAbilities.Describe(game.world, action);
            options.Add(described with
            {
                Verb = ActionVerb,
                Id = game.Handle(
                    $"{ActionVerb}:{action.Type}:{action.Ordinal}:player:{action.Player}",
                    described.AnchorId),
            });
        }
    }

    private static void AddIdentityPower(Game game, Seat seat, List<Affordance> options)
    {
        if (!Forms.In(game.world, seat, game.facts, Forms.Hero))
        {
            if (BasicPowers.CanRecover(game.world, game.facts, seat.Index))
                options.Add(game.Anchored(BasicPowers.RecoverVerb, seat));
            return;
        }
        if (BasicPowers.CanUsePower(game.facts, seat.IdentityCard, "ATK"))
            game.Offer(options, seat.IdentityCard, BasicPowers.AttackVerb,
                BasicPowers.Attackable(game.world, game.facts, seat.Index));
        if (BasicPowers.CanUsePower(game.facts, seat.IdentityCard, "THW"))
            game.Offer(options, seat.IdentityCard, BasicPowers.ThwartVerb,
                BasicPowers.Thwartable(game.world, game.facts, seat.Index));
    }

    /// <summary>
    /// Actions available directly or by game.asking another player —
    /// <c>rr:player-turn.5</c> and <c>rr:player-turn.6</c>.
    /// </summary>
    /// <remarks>
    /// The active player's actions come first, followed clockwise by the other
    /// players. The rules do not order offers from several players; this stable
    /// ordering is the engine's choice. Encounter-card actions remain distinct
    /// per eligible player because the acting player supplies the form,
    /// resources, targets and per-player limits used to resolve the action.
    /// </remarks>
    internal static IEnumerable<PendingAbility> TurnActions(this Game game)
    {
        for (int offset = 0; offset < game.world.Players; offset++)
        {
            int player = (game.Active + offset) % game.world.Players;
            if (game.world.Seats[player].Eliminated)
            {
                continue;
            }

            foreach (var action in game.world.ActionAbilities.Actions(game.world, player))
            {
                yield return action;
            }
        }
    }

    /// <summary>Offers one card in hand, anchored to the card rather than the seat.</summary>
    /// <remarks>
    /// A play is clicked on the card, not on the identity, so this is the one
    /// affordance whose anchor is not <see cref="Seat.IdentityCard"/>. The
    /// handle is cached on <c>(verb, anchor)</c> like any other, which gives a
    /// card in hand a stable id across the re-offers of one turn.
    /// </remarks>
    internal static Affordance Priced(this Game game,
        Seat seat, Card card, CostOption price, IReadOnlyList<int>? attachmentTargets)
    {
        int anchor = card.ObjectId;

        return new Affordance(
            Id: game.Handle(CardPlay.Verb, anchor),
            Verb: CardPlay.Verb,
            AnchorId: anchor,
            AnchorPlayer: seat.Index,
            Label: CardPlay.Verb,

            // The identity, exactly one, and that is measured rather than
            // reasoned: every recorded `Play` affordance carries
            // `targets: {legal: [1], min: 1, max: 1}` where 1 is the identity
            // card. It reads as "into whose play area", which is a real choice
            // at more than one player even though the card is the anchor.
            Targets: attachmentTargets is not null
                ? new TargetRequest(attachmentTargets, Min: 1, Max: 1)
                : new TargetRequest([seat.IdentityCard.ObjectId], Min: 1, Max: 1),
            Costs: [price]);
    }

    /// <summary>Offers a basic power if it has a target or a status to clear.</summary>
    /// <remarks>
    /// Anchored to the character using it, which is the identity for a hero's
    /// own power and the ally for <c>rr:player-turn.4</c>. Two allies attacking
    /// are two options, because <c>rr:ally.2</c> permits "any number".
    /// </remarks>
    internal static void Offer(this Game game,
        List<Affordance> options, Card character, string verb, IReadOnlyList<Card> targets)
    {
        string cancellingStatus = string.Equals(
            verb, BasicPowers.AttackVerb, StringComparison.Ordinal)
                ? Statuses.Stunned
                : Statuses.Confused;
        bool cancelledByStatus = Statuses.Afflicted(
            game.world, game.facts, character, cancellingStatus);
        bool targetlessStatusAttempt = targets.Count == 0 && cancelledByStatus;
        if (targets.Count == 0 && !targetlessStatusAttempt)
        {
            return;
        }

        long power = StateFields.Modified(
            game.world, character,
            string.Equals(verb, BasicPowers.AttackVerb, StringComparison.Ordinal)
                ? "attack"
                : "thwart",
            game.facts, game.world.Players);
        bool ranged = StateFields.Modified(game.world, character, "ranged", game.facts, game.world.Players) > 0;
        options.Add(game.Anchored(verb, character, game.world.Seats[game.Active]) with
        {
            Description = PowerDescription(game, character, verb, power, ranged,
                cancelledByStatus, cancellingStatus),
            // Exactly one target: `rr:attack-player-ability-type.1` and
            // `rr:thwart.1` are each one enemy or one scheme. An ability that
            // hits several is a different thing (`.5`) and is not a basic power.
            Targets = new TargetRequest(
                [.. targets.Select(target => target.ObjectId)],
                Min: targetlessStatusAttempt ? 0 : 1,
                Max: targetlessStatusAttempt ? 0 : 1)
            {
                Details = targets.ToDictionary(target => target.ObjectId,
                    target => PowerTargetDetail(game, character, target, verb, power,
                        cancelledByStatus, cancellingStatus)),
            },
        });
    }

    private static string PowerDescription(Game game, Card character, string verb,
        long power, bool ranged, bool cancelled, string status) =>
        $"{game.facts.Title(character.FaceId)} · {verb} for {power}"
        + (ranged && verb == BasicPowers.AttackVerb ? " · Ranged" : string.Empty)
        + (cancelled ? $" · {status} cancels this attempt and is discarded" : string.Empty);

    private static string PowerTargetDetail(Game game, Card character, Card target,
        string verb, long power, bool cancelled, string status)
    {
        if (!cancelled) return game.BasicPowerTargetDetail(character, target, verb, power);
        string effect = verb == BasicPowers.AttackVerb
            ? "damage will be dealt" : "threat will be removed";
        return $"{status} cancels this attempt; no {effect}";
    }

    internal static string BasicPowerTargetDetail(this Game game,
        Card character, Card target, string verb, long power)
    {
        if (string.Equals(verb, BasicPowers.ThwartVerb, StringComparison.Ordinal))
        {
            long current = target.Tokens.GetValueOrDefault("k_threat");
            long result = Math.Max(0, current - power);
            long threshold = game.facts.PrintedValue(target.FaceId, "TargetThreat", game.world.Players);
            return threshold > 0
                ? $"{current}/{threshold} → {result}/{threshold} threat"
                : $"{current} → {result} threat";
        }

        return Damage.PreviewAttack(game.world, game.facts, character, character, target, power);
    }

    internal static Prompt EndPhasePrompt(this Game game)
    {
        var seat = game.world.Seats[game.Active];
        return new Prompt(
            Player: seat.Index,
            Asking: Question.TurnOption,
            When: Timing.TimingPriority.Untimed,
            Trigger: EndPhaseTrigger,
            Label: $"{seat.Name} End Phase",
            Cancellable: false,
            Affordances:
            [
                // `rr:end-of-player-phase.step.1` is two clauses, and the
                // second is a floor: a player "**must** discard down to their
                // hand size if they have more cards than their hand size". So
                // an over-full hand cannot answer with nothing, and the
                // affordance has to say so — `PhaseEnd.DiscardToHandSize`
                // refuses an answer that leaves too many, and an engine that
                // offers what it will refuse has told the client a lie.
                game.HandChoice(
                    seat,
                    EndPhaseVerb,
                    Math.Max(
                        0,
                        seat.Hand.Cards.Count - (int)PhaseEnd.HandSize(game.world, seat, game.facts))),
            ]);
    }

    /// <summary>An affordance offering some number of the player's hand.</summary>
    /// <remarks>
    /// The mulligan and the end phase are nearly the same shape: choose between
    /// <paramref name="least"/> and all of your hand. They differ only in the
    /// floor — <c>rr:appendix-ii-setup.step.15</c> lets a player mulligan "any
    /// number of cards", including none, while the end of the player phase has
    /// a hand size to come down to.
    /// <para>
    /// The candidate list is the hand in its own order, not sorted — the
    /// recorded offer is <c>[42, 45, 37, 9, 47, 46]</c>, which is the hand read
    /// bottom to top, and sorting it would change which card a client
    /// highlights first.
    /// </para>
    /// </remarks>
    internal static Affordance HandChoice(this Game game, Seat seat, string verb, int least = 0)
    {
        var hand = new int[seat.Hand.Cards.Count];
        for (int index = 0; index < hand.Length; index++)
        {
            hand[index] = seat.Hand.Cards[index].ObjectId;
        }

        return game.Anchored(verb, seat) with
        {
            Targets = new TargetRequest(
                Legal: hand,
                Min: least,
                Max: hand.Length,
                // This is a presentation marker for choosing from a card
                // collection rather than clicking cards already laid out on
                // the table. The cooperative product exposes player hands by
                // default, so it is not itself an information boundary.
                IsSearch: true),
        };
    }

    internal static Affordance Anchored(this Game game, string verb, Seat seat) =>
        game.Anchored(verb, seat.IdentityCard, seat);

    /// <summary>
    /// The stable handle for one option, allocating it the first time.
    /// </summary>
    /// <remarks>
    /// <b>Every affordance in a prompt this class builds comes through here</b>,
    /// and that is the point rather than tidiness. <c>Affordance.Id</c> is what
    /// <see cref="GameResolution.Resolve"/> looks the answer up by, and it looks it up with
    /// <c>First</c> — so two options sharing an id in one prompt do not fail,
    /// they silently resolve the wrong one. An ability's own affordance arrives
    /// carrying the card's object id (<c>IAbilityDescriptions.Describe</c> has no
    /// allocator to ask), and a card play carries a counter, and the two number
    /// spaces overlap, so this allocator keeps them disjoint.
    /// </remarks>
    /// <param name="game">The game.</param>
    /// <param name="verb">What kind of option it is.</param>
    /// <param name="anchor">The board object it hangs on.</param>
    internal static int Handle(this Game game, string verb, int anchor)
    {
        if (!game.handles.TryGetValue((verb, anchor), out int id))
        {
            id = game.nextHandle++;
            game.handles[(verb, anchor)] = id;
        }

        return id;
    }

    /// <summary>An affordance anchored to a particular card.</summary>
    internal static Affordance Anchored(this Game game, string verb, Card on, Seat seat)
    {
        int anchor = on.ObjectId;

        return new Affordance(
            Id: game.Handle(verb, anchor),
            Verb: verb,
            AnchorId: anchor,
            AnchorPlayer: seat.Index,
            // Verb and label are the same string for a derived affordance.
            // There is no second source to fill `Label` from, and filling it
            // from anywhere else would be inventing it -- a client that wants
            // richer wording can build it from the verb and the anchor.
            Label: verb);
    }
}
