using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>
/// A card's cost after modifiers, together with the one-use effects that
/// produced it.
/// </summary>
/// <remarks>
/// Kept as data because determining a cost and paying it are separate steps of
/// <c>rr:initiating-abilities</c>. The effects are consumed only after the card
/// has successfully been played; merely describing an affordance does not use
/// them.
/// </remarks>
/// <param name="Amount">The cost after modifiers, never less than zero.</param>
/// <param name="Modifiers">The effects applied while determining it.</param>
public sealed record AdjustedCardCost(
    long Amount, IReadOnlyList<ContinuousEffect> Modifiers);

/// <summary>
/// Playing a card from hand — <c>rr:play-put-into-play</c>,
/// <c>rr:initiating-abilities</c>.
/// </summary>
/// <remarks>
/// <para>
/// "Playing a card involves paying the card's cost and placing the card in the
/// play area. This causes the card to enter play <i>(or, in the case of an
/// event card, to resolve its ability and be placed in the discard pile)</i>."
/// </para>
/// <para>
/// <c>rr:initiating-abilities</c> numbers seven steps and they are numbered
/// here, because the order is what the rule is: restrictions are checked before
/// the cost is worked out, the cost is worked out before it is paid, and
/// <b>step 5 aborts without paying anything</b> if it cannot all be paid.
/// </para>
/// </remarks>
public static class CardPlay
{
    /// <summary>The affordance verb for playing a card.</summary>
    /// <remarks>
    /// <c>rr:player-turn.2</c> is "play a card", so the verb is <c>Play</c>.
    /// The most common one on the wire by a wide margin, and a client renders
    /// it, so it is a contract like the basic powers in
    /// <see cref="BasicPowers"/>.
    /// </remarks>
    public const string Verb = "Play";

    /// <summary>The lasting-effect kind for reducing a player's next card cost.</summary>
    public const string CardCostReduction = "cardCostReduction";

    /// <summary>
    /// Makes the next card one player plays this phase cost less.
    /// </summary>
    /// <remarks>
    /// <c>rr:lasting-effects.1</c> keeps the effect after the creating ability
    /// resolves. Its two bounds are independent: playing a card spends its one
    /// use, while <c>rr:lasting-effects.5</c> expires it at the end of the
    /// player phase if no card was played. The affected identity names the
    /// player without introducing another seat-shaped field on
    /// <see cref="ContinuousEffect"/>.
    /// </remarks>
    public static ContinuousEffects.Registration ReduceNextCardCost(
        World world, Card source, int player, long amount)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(player);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(player, world.Players);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);

        return world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            CardCostReduction,
            Amount: amount,
            Card: source.ObjectId,
            Affects: world.Seats[player].IdentityCard.ObjectId,
            Lasts: new Duration(Until: TimingPoints.EndOfPlayerPhase, Uses: 1)));
    }

    /// <summary>
    /// Determines one card's modified resource cost and remembers the effects
    /// that supplied the adjustment.
    /// </summary>
    /// <remarks>
    /// <c>rr:initiating-abilities.step.3</c> determines the cost and step 4
    /// applies modifiers before step 5 pays it. Cost cannot become negative;
    /// each reduction stops at zero. This method does not consume anything,
    /// because pricing a card is not playing it.
    /// </remarks>
    public static AdjustedCardCost CostOf(
        World world, ICardFacts facts, Seat seat, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);

        long amount = Resources.Cost(card.FaceId, facts, world.Players) ?? 0;
        var modifiers = world.Effects.Active().Where(effect =>
            string.Equals(effect.Kind, CardCostReduction, StringComparison.Ordinal)
            && effect.Affects == seat.IdentityCard.ObjectId
            && effect.Amount > 0).ToList();

        foreach (var modifier in modifiers)
        {
            amount = Math.Max(0, amount - modifier.Amount);
        }

        return new AdjustedCardCost(amount, modifiers);
    }

    /// <summary>Consumes the one-use effects applied to a successfully played card.</summary>
    public static void UseCostModifiers(World world, AdjustedCardCost cost)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(cost);

        foreach (var modifier in cost.Modifiers)
        {
            world.Effects.Use(modifier);
        }
    }

    /// <summary>
    /// Every card in a hand that can be spent for resources.
    /// </summary>
    /// <remarks>
    /// <c>rr:resource.1</c> — "discarding cards from their hand to generate the
    /// resource or resources indicated at the bottom-left corner of the card".
    /// A card printing nothing there generates nothing and is not a generator.
    /// <para><c>rr:resource-ability</c>'s resource abilities are included from
    /// cards in play beside these hand cards.</para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="seat">Whose hand.</param>
    /// <param name="payingFor">The card being paid for, or null for an ability cost.</param>
    public static IReadOnlyList<ResourceSource> Generators(
        World world, ICardFacts facts, Seat seat, Card? payingFor = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(seat);

        // `rr:resource-ability.1` -- one "can be triggered **anytime the player
        // who controls the ability is generating resources to pay a cost**", so
        // it belongs beside the cards in hand: another way to make a resource
        // rather than another moment. Peter Parker's "Scientist" is the one the
        // recorded prompt carries, and it is why that prompt lists six
        // generators for five payable cards.
        var sources = new List<ResourceSource>(world.Abilities.ResourceAbilities(
            world, seat.Index));

        foreach (var card in seat.Hand.Cards)
        {
            string generates = world.Abilities.ResourcesGeneratedBy(world, card, payingFor);
            if (generates.Length > 0)
            {
                sources.Add(new ResourceSource(card.ObjectId, generates));
            }
        }

        return sources;
    }

    /// <summary>
    /// Whose hands may pay for one card — <c>rr:alliance</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "When a player declares their intention to play a card with the alliance
    /// keyword, <b>any player(s) may help pay the costs</b> for that card",
    /// which <c>.1</c> writes as the constant ability "while paying costs for
    /// this card, any player may contribute to paying those costs".
    /// </para>
    /// <para>
    /// <c>rr:alliance.2</c> is the limit of it: "only the player playing the
    /// card with the alliance keyword is considered to be resolving that
    /// card". Helping to pay is not playing — the card is still the one
    /// player's, and everything downstream of the payment reads the seat that
    /// played it.
    /// </para>
    /// <para>
    /// Every spent card goes to <b>its own owner's</b> discard pile, which
    /// <see cref="Discard.Card"/> already does by reading the card rather than
    /// the player who spent it.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="seat">Who is playing the card.</param>
    /// <param name="card">The card being paid for.</param>
    public static IReadOnlyList<Seat> Paying(
        World world, ICardFacts facts, Seat seat, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);

        return StateFields.Modified(world, card, "alliance", facts, world.Players) > 0
            ? [.. world.PlayerOrder.Select(index => world.Seats[index])]
            : [seat];
    }

    /// <summary>
    /// What a card in hand costs, and what could pay for it — or null when it
    /// cannot be played at all.
    /// </summary>
    /// <remarks>
    /// <c>rr:initiating-abilities.step.2</c> and <c>.step.3</c>: the play
    /// restrictions are checked first, then the cost and "the player's ability
    /// to pay them". A card that fails either is not offered — an affordance
    /// that would throw when taken is worse than an absent one.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="seat">Whose card.</param>
    /// <param name="card">The card in hand.</param>
    public static CostOption? Price(World world, ICardFacts facts, Seat seat, Card card)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);

        if (!Permitted(world, facts, seat, card, abilities: world.Abilities))
        {
            return null;
        }

        // **Events are not a turn option on their own.** `rr:player-turn.5`
        // reaches them through "trigger an **Action** ability on an event card
        // in their hand", so an event without one is played in a window and not
        // here -- 555 of the 602 events in the pool have no Action ability at
        // all. An authored Action event is offered by `ICardAbilities.Actions`
        // instead, which plays it while resolving that action.
        //
        // The recorded board is the check: its opening hand holds `01003`
        // Backflip, whose ability is an **Interrupt (defense)**, and the
        // recording does not offer it.
        if (facts.Kind(card.FaceId) == CardKind.Event)
        {
            return null;
        }

        long cost = CostOf(world, facts, seat, card).Amount;

        // The card being played cannot also pay for itself: `rr:cost.3` spends
        // resources "by discarding cards from their hand", and this one is
        // leaving the hand to be played.
        var sources = Paying(world, facts, seat, card)
            .SelectMany(paying => Generators(world, facts, paying, card))
            .Where(source => source.Effect != card.ObjectId)
            .ToList();

        // **Asked of the whole pool, and that is exactly the right question.**
        // `rr:cost.4` permits generating beyond the cost, so if every generator
        // together cannot pay then no choice among them can; and if they can,
        // spending all of them is a payment. The types matter as well as the
        // count -- `rr:requirement-resources` makes a card with a requirement
        // unplayable without those resources, however many cards are in hand.
        string pool = string.Concat(sources.SelectMany(source => source.Generates));
        return !Resources.Pays(pool, cost, Resources.Required(world, card, facts))
            ? null
            : new CostOption(
                Target: card.ObjectId,
                Cost: cost.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Sources: sources);
    }

    /// <summary>
    /// Plays a card — <c>rr:initiating-abilities</c> steps 1 to 7.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do.</param>
    /// <param name="seat">Who is playing it.</param>
    /// <param name="card">The card.</param>
    /// <param name="paying">The cards discarded to pay, by object id.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="targets">The chosen attachment host, when the card names one.</param>
    /// <exception cref="RulesNotImplementedException">
    /// A restriction is not met, the payment does not cover the cost, or the
    /// card needs a rule this engine does not have.
    /// </exception>
    public static void Play(
        World world, ICardFacts facts, ICardAbilities abilities, Seat seat, Card card,
        IReadOnlyList<int> paying, List<GameEvent> events,
        IReadOnlyList<int>? targets = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(paying);
        ArgumentNullException.ThrowIfNull(events);

        // Step 1 is the card going faceup on the table, and `.step.1` says in
        // as many words that it "is not in play" there. Nothing on this board
        // can observe that instant, so it is not modelled -- but a card that
        // fails step 2 has to still be in hand, which it is.
        //
        // Step 2. Play restrictions.
        if (!Permitted(world, facts, seat, card, targets, abilities))
        {
            throw new RulesNotImplementedException(
                $"card {card.ObjectId} ('{card.FaceId}') in {card.Area.Type} cannot be played "
                + $"by {seat.Name} right now");
        }

        // Steps 3 and 4. Determine the cost and apply modifiers. The same
        // operation prices the affordance, so what was offered and what is
        // charged cannot disagree.
        var adjusted = CostOf(world, facts, seat, card);

        // Step 5. Pay it -- "if this step is reached and the cost(s) cannot be
        // paid, **abort this process without paying any costs**", so the whole
        // payment is checked before a single card is discarded.
        // `rr:alliance` -- for an alliance card this is every player's hand,
        // and for every other card it is only this player's.
        var hands = Paying(world, facts, seat, card).Select(player => player.Hand).ToList();

        Spend(
            world, facts, hands, paying, adjusted.Amount, Resources.Required(world, card, facts),
            card.ObjectId, seat.Index, events, payingFor: card);

        // Steps 6 and 7. The card is played: it enters play, or it is an event
        // and its ability resolves before it is discarded.
        Enter(world, facts, abilities, seat, card, events, targets ?? []);

        // Playing, not pricing or attempting to pay, spends "the next card"
        // effect. The snapshot excludes a discount the played card itself may
        // have created while entering play.
        UseCostModifiers(world, adjusted);
    }

    /// <summary>Plays a card while an effect ignores its resource cost.</summary>
    /// <remarks>
    /// This is a distinct entry point because ignoring a cost is a permission
    /// supplied by another resolving ability, not a payment choice on the
    /// card's ordinary affordance. All play restrictions still apply.
    /// </remarks>
    public static void PlayIgnoringResourceCost(
        World world, ICardFacts facts, ICardAbilities abilities, Seat seat, Card card,
        List<GameEvent> events, IReadOnlyList<int>? targets = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        if (facts.Kind(card.FaceId) == CardKind.Event)
        {
            throw new RulesNotImplementedException(
                $"event '{card.FaceId}' cannot be played ignoring its resource cost "
                + "until its Action selection can be represented");
        }

        if (!Permitted(world, facts, seat, card, targets, abilities))
        {
            throw new RulesNotImplementedException(
                $"card {card.ObjectId} ('{card.FaceId}') cannot be played ignoring its cost");
        }

        // `rr:requirement-resources.2`: ignoring the cost generates and pays
        // no resources, so a printed requirement makes this permission
        // unusable. Refuse before the card leaves its hand.
        if (Resources.Required(world, card, facts).Length > 0)
        {
            throw new RulesNotImplementedException(
                $"card '{card.FaceId}' has a resource requirement and cannot be played "
                + "ignoring its resource cost");
        }

        // `rr:ignore.1`: zero resources are considered paid. Cost reductions
        // are neither applied nor consumed because there is no resource cost
        // in effect during this play.
        Enter(world, facts, abilities, seat, card, events, targets ?? []);
    }

    /// <summary>Plays an owned card from an out-of-play zone by permission.</summary>
    /// <remarks>
    /// <c>rr:play-restrictions-and-permissions.2</c> allows a permission to
    /// override the normal timing or source-zone specification; its example is
    /// an ally played from a discard pile. The caller is the resolving effect
    /// that supplied that permission. Printed restrictions and the ordinary
    /// resource cost remain in force.
    /// </remarks>
    public static void PlayWithPermission(
        World world, ICardFacts facts, ICardAbilities abilities, Seat seat, Card card,
        IReadOnlyList<int> paying, List<GameEvent> events,
        IReadOnlyList<int>? targets = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(paying);
        ArgumentNullException.ThrowIfNull(events);

        if (facts.Kind(card.FaceId) == CardKind.Event)
        {
            throw new RulesNotImplementedException(
                $"event '{card.FaceId}' cannot use an out-of-zone play permission "
                + "until its Action selection can be represented");
        }

        if (!Permitted(
                world, facts, seat, card, targets, abilities,
                outOfPlayPermission: true))
        {
            throw new RulesNotImplementedException(
                $"card {card.ObjectId} ('{card.FaceId}') cannot use this play permission");
        }

        var adjusted = CostOf(world, facts, seat, card);
        var hands = Paying(world, facts, seat, card).Select(player => player.Hand).ToList();
        Spend(
            world, facts, hands, paying, adjusted.Amount,
            Resources.Required(world, card, facts), card.ObjectId, seat.Index,
            events, payingFor: card);
        Enter(world, facts, abilities, seat, card, events, targets ?? []);
        UseCostModifiers(world, adjusted);
    }

    /// <summary>
    /// Puts an ally into play under the named player's control.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:play-put-into-play</c> ignores the ally's resource cost and the
    /// ordinary restrictions on playing it, then places it in its controller's
    /// play area. The card's <see cref="Card.Owner"/> does not change; if it
    /// later leaves play, <c>rr:ownership-and-control.7.2</c> sends it to its
    /// owner's corresponding out-of-play area.
    /// </para>
    /// <para>
    /// <c>rr:play-put-into-play.3</c> says this is not playing the card, so no
    /// <c>CardPlayed</c> step is scheduled. It still enters play, and therefore
    /// uses the same entry lifecycle as a card played from hand.
    /// </para>
    /// </remarks>
    public static void PutAllyIntoPlay(
        World world, ICardFacts facts, ICardAbilities abilities, Card ally,
        int controller, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(ally);
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentOutOfRangeException.ThrowIfNegative(controller);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(controller, world.Players);

        if (facts.Kind(ally.FaceId) != CardKind.Ally)
        {
            throw new RulesNotImplementedException(
                $"card {ally.ObjectId} is not an ally and cannot enter an allies area");
        }

        if (DeckTypes.IsInPlay(ally.Area.Type))
        {
            throw new RulesNotImplementedException(
                $"ally {ally.ObjectId} is already in play and cannot be put into play again");
        }

        if (facts.FormKeyword(ally.FaceId) is { } form
            && !Forms.In(world, world.Seats[controller], facts, form))
        {
            throw new RulesNotImplementedException(
                $"ally {ally.ObjectId} can only be put into play in {form} form");
        }

        var from = ally.Area;
        int previousController = from.PlayArea.IsPlayers
            ? from.PlayArea.Player
            : ally.Owner;
        var into = world.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(controller), cardOwner: ally.Owner);

        World.MoveToTop(ally, into);
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(into),
            [new Landing(ally.ObjectId, into.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Put_Into_Play",
        });

        if (previousController != controller)
        {
            events.Add(new ControlChanged(ally.ObjectId, previousController, controller)
            {
                Trigger = trigger, Verb = "Put_Into_Play",
            });
        }

        if (AllyLimit(world, facts, world.Seats[controller], ally))
        {
            FinalizeAllyEntry(world, ally, controller);
        }
        else
        {
            Reveal.EnterPlay(world, facts, ally, events, abilities: abilities);
        }
    }

    /// <summary>
    /// Spends resources to pay a cost — <c>rr:initiating-abilities.step.5</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "If this step is reached and the cost(s) cannot be paid, <b>abort this
    /// process without paying any costs</b>", so the whole payment is checked
    /// before a single card is discarded.
    /// </para>
    /// <para>
    /// Shared by a card being played and an ability being triggered, because
    /// <c>rr:cost</c> is one rule for both — "a cost is anything a player must
    /// do or pay in order to initiate an ability", and playing a card is
    /// initiating one.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="hands">Whose hands may pay — see <see cref="Paying"/>.</param>
    /// <param name="paying">The cards discarded to pay, by object id.</param>
    /// <param name="cost">How many resources are needed.</param>
    /// <param name="required">Specific types that must be among them, or empty.</param>
    /// <param name="itself">
    /// A card that cannot pay for itself, or <c>-1</c>. <c>rr:cost.3</c> spends
    /// resources "by discarding cards from their hand", and a card leaving the
    /// hand to be played is not also in it.
    /// </param>
    /// <param name="payer">
    /// Whose cost it is, for a generator that is an ability rather than a card.
    /// </param>
    /// <param name="events">Where to record what moved.</param>
    /// <param name="payingFor">The card being paid for, or null for an ability cost.</param>
    /// <param name="resourcePayers">
    /// Which player owns each selected resource ability. Ordinary payments
    /// omit this because every ability belongs to <paramref name="payer"/>;
    /// alliance payments may name a helper's ability.
    /// </param>
    public static void Spend(
        World world, ICardFacts facts, IReadOnlyList<Area> hands, IReadOnlyList<int> paying,
        long cost, string required, int itself, int payer, List<GameEvent> events,
        Card? payingFor = null, IReadOnlyDictionary<int, int>? resourcePayers = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(hands);
        ArgumentNullException.ThrowIfNull(paying);
        ArgumentNullException.ThrowIfNull(events);

        var spent = new List<Card>();
        var generated = new System.Text.StringBuilder();

        // `rr:resource-ability` -- a generator that is not a card in hand is an
        // ability on a card in play, and using one is not discarding it.
        // `rr:cost.3` spends resources "by discarding cards from their hand",
        // which is the *other* way and not the only one.
        //
        // Asked as "was this one offered" rather than "is this card in a hand",
        // so that a payment naming a card that is neither still says the thing
        // that is wrong with it.
        var abilityPayers = resourcePayers is null
            ? world.Abilities.ResourceAbilities(world, payer)
                .GroupBy(source => source.Effect)
                .ToDictionary(group => group.Key, _ => payer)
            : resourcePayers;

        foreach (int id in paying)
        {
            var source = world.Cards[id];
            if (abilityPayers.TryGetValue(id, out int abilityPayer)
                && !hands.Contains(source.Area))
            {
                generated.Append(world.Abilities.UseResource(
                    world, abilityPayer, id, events));
                continue;
            }

            if (!hands.Contains(source.Area))
            {
                // Named, because "not in a hand" and "not in *yours*" are
                // different mistakes and only one of them is about alliance.
                throw new RulesNotImplementedException(
                    hands.Count == 1 && hands[0].CardOwner >= 0
                        ? $"card {id} is not in {world.Seats[hands[0].CardOwner].Name}'s hand "
                          + "and cannot be spent from it"
                        : $"card {id} is in no player's hand and cannot be spent from one");
            }

            if (source.ObjectId == itself)
            {
                throw new RulesNotImplementedException(
                    $"card {id} is being played and cannot also pay for itself");
            }

            spent.Add(source);
            generated.Append(world.Abilities.ResourcesGeneratedBy(world, source, payingFor));
        }

        if (!Resources.Pays(generated.ToString(), cost, required))
        {
            throw new RulesNotImplementedException(
                $"the cost is {cost}"
                + (required.Length > 0 ? $" requiring '{required}'" : string.Empty)
                + $" and the payment generates '{generated}'; "
                + "rr:initiating-abilities.step.5 aborts without paying");
        }

        foreach (var source in spent)
        {
            Discard.Card(world, source, Verb, events);
        }
    }

    /// <summary>
    /// <c>rr:initiating-abilities.step.2</c> — the play restrictions.
    /// </summary>
    /// <remarks>
    /// Only the ones the rules state generally. A card's own restrictions are
    /// its text, which is <c>src/Marvel.Cards</c>'s business, and a card that
    /// has one is not filtered here — it fails when its ability runs.
    /// </remarks>
    /// <summary>
    /// Whether a team-up card's two characters are both on the table —
    /// <c>rr:team-up</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "A card with the team-up keyword cannot be played unless <b>both</b> of
    /// the named friendly characters <i>(identity or ally)</i> are in play",
    /// which <c>.1</c> writes out as two separate requirements — one character
    /// matching name 1 and one matching name 2.
    /// </para>
    /// <para>
    /// <b>"Friendly" is every player's, not yours.</b> <c>rr:friendly</c> is
    /// one sentence — "a blanket term that refers to cards <b>the players</b>
    /// control" — so at a table the other player's Wasp is the Wasp this card
    /// needs. Twenty-eight cards print the keyword and every one of them names
    /// two heroes, which is the shape a solo game cannot satisfy without an
    /// ally.
    /// </para>
    /// <para>
    /// <c>rr:team-up.2</c>: "an ally counts as a named character if
    /// <b>either its title or subtitle</b> matches", so both are checked and
    /// on identities as well — an identity's two faces print different titles
    /// and only the faceup one is in play (<c>rr:identity.4</c>).
    /// </para>
    /// <para>
    /// The deck-building half of <c>rr:team-up.1</c> — "you cannot include this
    /// card in your deck unless your alter-ego or hero title matches" — is not
    /// here. The decks are given to this engine already built, and a rule about
    /// what may go in one has nothing to check at play time.
    /// </para>
    /// </remarks>
    private static bool TeamedUp(World world, ICardFacts facts, Card card)
    {
        if (Characteristics.IsLost(world, card, "team-up")
            || !facts.Attributes(card.FaceId).TryGetValue("TeamUp", out string? named))
        {
            return true;
        }

        var characters = world.Areas
            .Where(area => area.Type is DeckType.HeroArea or DeckType.AlliesArea)
            .SelectMany(area => area.Cards)
            .ToList();

        return named
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .All(name => characters.Any(character => Matches(facts, character, name)));
    }

    /// <summary>Whether one character in play is the character a name means.</summary>
    /// <remarks>
    /// <para>
    /// <c>rr:team-up.2</c> for the plain case: "an ally counts as a named
    /// character if <b>either its title or subtitle</b> matches". Only the
    /// faceup side of an identity is in play (<c>rr:identity.4</c>), so a
    /// player who has flipped down is not the hero the card names.
    /// </para>
    /// <para>
    /// <b>A slash names one character by two of its names.</b> "Heart of the
    /// Panther" prints <i>Team-Up (Black Panther/T'Challa and Black
    /// Panther/Shuri)</i>, because two identities share the hero title Black
    /// Panther and the alter-ego is what tells them apart. No card is titled
    /// "Black Panther/T'Challa", so the notation has to be read rather than
    /// matched.
    /// </para>
    /// <para>
    /// It is read against every one of the identity's faces, and
    /// <c>rr:unique-icon.1.2</c> is why that is not a liberty: the rules
    /// already use an identity's <b>alter-ego title</b> as one of its
    /// identifying names — "the identity with the T'Challa alter-ego, the
    /// T'Challa ally, and the Black Panther ally with the subtitle 'T'Challa'
    /// are all considered to match". Reading only the faceup side would make
    /// the notation name nothing at all, since neither face carries both
    /// halves.
    /// </para>
    /// </remarks>
    private static bool Matches(ICardFacts facts, Card character, string name)
    {
        var halves = name.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (halves.Length == 1)
        {
            return Named(facts, character.FaceId, name);
        }

        return halves.All(half => character.Faces.Any(face => Named(facts, face, half)));
    }

    private static bool Named(ICardFacts facts, string faceId, string name) =>
        string.Equals(facts.Title(faceId), name, StringComparison.Ordinal)
        || string.Equals(facts.Subtitle(faceId), name, StringComparison.Ordinal);

    private static bool Permitted(
        World world, ICardFacts facts, Seat seat, Card card,
        IReadOnlyList<int>? targets = null, ICardAbilities? abilities = null,
        bool outOfPlayPermission = false)
    {
        if (!outOfPlayPermission && card.Area != seat.Hand)
        {
            // "Cards are played from a player's hand."
            return false;
        }
        if (outOfPlayPermission
            && (card.Owner != seat.Index || DeckTypes.IsInPlay(card.Area.Type)))
        {
            // A permission changes only the named source-zone/timing
            // specification. It neither takes another player's card nor turns
            // playing an already-in-play card into a new play.
            return false;
        }

        // `rr:dash-value.1`: a dash cost cannot be paid; the card may only
        // enter play through another effect. The dataset represents that both
        // as a literal dash and, on card types never normally played, as no
        // Cost field at all.
        if (!Resources.HasPlayableCost(card.FaceId, facts))
        {
            return false;
        }

        // `rr:play-put-into-play.1` and `rr:form-change-form.7`: cards with the
        // text "[type] form only" can only be played by a player whose identity
        // is in that form.
        if (facts.FormKeyword(card.FaceId) is { } form
            && !Forms.In(world, seat, facts, form))
        {
            return false;
        }

        // `rr:team-up` -- "a card with the team-up keyword cannot be played
        // unless both of the named friendly characters (identity or ally) are
        // in play".
        if (!TeamedUp(world, facts, card))
        {
            return false;
        }

        if (!WithinPerPlayerLimit(
                world, facts, seat, card, targets, abilities ?? world.Abilities))
        {
            return false;
        }

        // `rr:player-turn.2` lists what may be played from hand as a turn
        // option: "an ally, upgrade, support, or player side scheme card".
        // **A resource card is not among them** -- `rr:resource-card` says its
        // "primary function is to be discarded from a player's hand to generate
        // resources", and the pool agrees: `01088` Energy prints no cost at all.
        //
        // An event is played a different way, through `rr:player-turn.5.d`
        // ("trigger an Action ability on an event card in their hand, by
        // playing that event"), which is why it is here and not offered by
        // `Price`.
        return facts.Kind(card.FaceId) is CardKind.Ally or CardKind.Upgrade
            or CardKind.Support or CardKind.Event;
    }

    /// <summary>Checks a printed “Max N per player” against the destination controller.</summary>
    private static bool WithinPerPlayerLimit(
        World world, ICardFacts facts, Seat seat, Card card, IReadOnlyList<int>? targets,
        ICardAbilities abilities)
    {
        long maximum = facts.PrintedValue(card.FaceId, "MaxPerUnit", world.Players);
        if (maximum <= 0)
        {
            return true;
        }

        // The extractor preserves which printed unit the maximum names.
        // Synthetic facts written before that field existed mean "per player";
        // this is an engine compatibility choice, not a Rules Reference term.
        string unit = facts.Attributes(card.FaceId)
            .GetValueOrDefault("MaxPerUnitKind", "player");
        IReadOnlyList<int>? eligible = facts.Kind(card.FaceId) == CardKind.Upgrade
            ? abilities.AttachmentTargets(world, card)
            : null;
        if (string.Equals(unit, "player", StringComparison.Ordinal))
        {
            if (eligible is null)
            {
                return CountControlled(world, facts, card, seat.Index) < maximum;
            }

            IEnumerable<int> controlledHosts = targets is { Count: > 0 } ? targets : eligible;
            return controlledHosts.Any(host =>
                eligible.Contains(host)
                && world.Cards[host].Area.PlayArea is { IsPlayers: true } area
                && CountControlled(world, facts, card, area.Player) < maximum);
        }

        // `rr:max-maximum.4`: every non-player unit is an attachment-host
        // maximum. AttachmentTargets has already applied the printed host
        // restriction (ally, enemy, scheme, and so on); this check asks the
        // separate question of whether that host already has this title.
        if (eligible is null && targets is not { Count: > 0 })
        {
            // A generic offer can precede attachment-target selection. There
            // is no host maximum to test until the ability layer supplies a
            // legal host; Play calls this method again with the chosen target.
            return true;
        }

        IEnumerable<int> hosts = targets is { Count: > 0 } ? targets : eligible!;
        return hosts.Any(host =>
            (eligible is null || eligible.Contains(host))
            && CountAttached(world, facts, card, host) < maximum);
    }

    /// <summary>Attachment targets that also satisfy the card's printed maximum.</summary>
    public static IReadOnlyList<int>? LegalAttachmentTargets(
        World world, ICardFacts facts, Seat seat, Card card, ICardAbilities abilities)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(seat);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(abilities);

        var targets = abilities.AttachmentTargets(world, card);
        return targets is null
            ? null
            : [.. targets.Where(target =>
                WithinPerPlayerLimit(world, facts, seat, card, [target], abilities))];
    }

    private static int CountControlled(
        World world, ICardFacts facts, Card card, int controller)
    {
        string title = facts.Title(card.FaceId);
        return world.Areas
            .Where(area => area.PlayArea == PlayArea.Of(controller))
            .SelectMany(area => area.Cards)
            .Count(inPlay => DeckTypes.IsInPlay(inPlay.Area.Type)
                && string.Equals(facts.Title(inPlay.FaceId), title, StringComparison.Ordinal));
    }

    private static int CountAttached(
        World world, ICardFacts facts, Card card, int host)
    {
        string title = facts.Title(card.FaceId);
        return world.Areas
            .Where(area => area.Host == host && DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Count(attached => string.Equals(
                facts.Title(attached.FaceId), title, StringComparison.Ordinal));
    }

    /// <summary>Transfers control of an in-play player card to another player.</summary>
    /// <remarks>
    /// The destination is validated before the card moves. In particular,
    /// <c>rr:max-maximum.3.1</c> forbids taking control of another copy of a
    /// “Max 1 per player” card already controlled there.
    /// </remarks>
    public static void TakeControl(
        World world, ICardFacts facts, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);
        if (player < 0 || player >= world.Seats.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(player));
        }
        if (!DeckTypes.IsInPlay(card.Area.Type) || !card.Area.PlayArea.IsPlayers)
        {
            throw new RulesNotImplementedException(
                $"card {card.ObjectId} is not an in-play player card whose control can transfer");
        }
        if (card.Area.PlayArea == PlayArea.Of(player))
        {
            return;
        }

        long maximum = facts.PrintedValue(card.FaceId, "MaxPerUnit", world.Players);
        string unit = facts.Attributes(card.FaceId)
            .GetValueOrDefault("MaxPerUnitKind", "player");
        if (maximum > 0
            && string.Equals(unit, "player", StringComparison.Ordinal)
            && CountControlled(world, facts, card, player) >= maximum)
        {
            throw new RulesNotImplementedException(
                $"player {player} already controls the maximum number of "
                + $"'{facts.Title(card.FaceId)}'");
        }

        var destination = world.AreaOf(
            card.Area.Type,
            PlayArea.Of(player),
            host: card.Area.Host,
            cardOwner: card.Owner);
        World.MoveToTop(card, destination);
    }

    /// <summary>Where a played card goes — <c>rr:enters-play</c>.</summary>
    private static void Enter(
        World world, ICardFacts facts, ICardAbilities abilities, Seat seat, Card card,
        List<GameEvent> events, IReadOnlyList<int> targets)
    {
        var kind = facts.Kind(card.FaceId);
        if (kind is CardKind.Event or CardKind.Resource)
        {
            // `rr:play-put-into-play.2`: "when an event card is played, place
            // it on the table, resolve its ability, and place the card in its
            // owner's discard pile." The ability *is* the card, so a card whose
            // ability nothing implements must say so rather than being a very
            // expensive discard.
            events.AddRange(abilities.WhenRevealed(world, card, seat.Index));
            Discard.Card(world, card, Verb, events);
            return;
        }

        int upgradeHost = seat.IdentityCard.ObjectId;
        var eligibleHosts = kind == CardKind.Upgrade
            ? abilities.AttachmentTargets(world, card)
            : null;
        if (eligibleHosts is not null)
        {
            if (targets.Count != 1 || !eligibleHosts.Contains(targets[0]))
            {
                throw new RulesNotImplementedException(
                    $"card {card.ObjectId} must attach to exactly one eligible target");
            }
            upgradeHost = targets[0];
        }

        // An encounter card's play area is the villain's, but attaching a
        // player upgrade to it does not give control to the scenario. The
        // exception in `rr:ownership-and-control.2.1` applies only when the
        // attached card is controlled by another *player*.
        var hostArea = world.Cards[upgradeHost].Area.PlayArea;
        int controller = kind == CardKind.Upgrade && hostArea.IsPlayers
            ? hostArea.Player
            : seat.Index;

        var into = kind switch
        {
            CardKind.Ally => world.AreaOf(
                DeckType.AlliesArea, PlayArea.Of(seat.Index), cardOwner: seat.Index),
            CardKind.Support => world.AreaOf(
                DeckType.SupportsArea, PlayArea.Of(seat.Index), cardOwner: seat.Index),

            // `rr:upgrade`: an upgrade attaches to a card, and with no ability
            // saying otherwise that card is the identity playing it.
            // `rr:ownership-and-control.2.1`: when it attaches to another
            // player's card, that player controls the upgrade, so it enters
            // that player's play area while retaining its original owner.
            CardKind.Upgrade => world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(controller),
                host: upgradeHost, cardOwner: seat.Index),
            _ => throw new RulesNotImplementedException(
                $"a {kind} played from hand has nowhere to enter play"),
        };

        var from = card.Area;
        World.MoveToTop(card, into);
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(into),
            [new Landing(card.ObjectId, into.Cards.Count - 1)])
        {
            Trigger = Verb, Verb = Verb,
        });

        if (kind == CardKind.Upgrade)
        {
            events.Add(new CardAttached(card.ObjectId, upgradeHost)
            {
                Trigger = Verb, Verb = Verb,
            });
        }

        // `rr:enters-play`: the keywords that fire when a card enters play do
        // not care how it got there. Eighteen allies in the pool print
        // `rr:toughness`, and before this a played one got no tough status card
        // -- only a *revealed* card ran them.
        if (AllyLimit(world, facts, seat, card))
        {
            FinalizeAllyEntry(world, card, seat.Index);
        }
        else
        {
            Reveal.EnterPlay(world, facts, card, events, abilities: abilities);
        }
        Played(world, seat, card);
        Restricted(world, facts, seat, card, events);
    }

    /// <summary>
    /// Ask which ally leaves when a player exceeds their ally limit —
    /// <c>rr:ally-limit</c>.
    /// </summary>
    private static bool AllyLimit(World world, ICardFacts facts, Seat seat, Card played)
    {
        if (FacedownDrones.Kind(played, facts) != CardKind.Ally)
        {
            return false;
        }

        return CheckAllyLimit(world, facts, seat.Index, played.ObjectId);
    }

    /// <summary>Schedule the mandatory choice when a player exceeds their ally limit.</summary>
    internal static bool CheckAllyLimit(
        World world, ICardFacts facts, int player, int subject = -1)
    {
        var seat = world.Seats[player];
        // Discard is also used while building deliberately partial boards.
        // With no identity assigned there is no player ally-limit value yet.
        if (seat.IdentityCard is null)
        {
            return false;
        }

        int controlled = world.Areas
            .Where(area => area.Type == DeckType.AlliesArea
                && area.PlayArea == PlayArea.Of(player))
            .Sum(area => area.Cards.Count);
        long limit = StateFields.Modified(
            world, seat.IdentityCard, "ally_limit", facts, world.Players);
        if (controlled <= limit)
        {
            return false;
        }

        // This is a mandatory rule choice rather than an occurrence, so it
        // opens no interrupt or response windows. It is scheduled before the
        // CardPlayed occurrence: the rule says the discard happens before
        // abilities that resolve upon entering play.
        if (!world.Agenda.Outstanding.Any(step =>
                step.What == Steps.ChooseAllyForLimit && step.Seat == player))
        {
            world.Agenda.Then(new PhaseStep(
                Steps.ChooseAllyForLimit,
                world.Agenda.Current?.Round ?? 0,
                0,
                Subject: subject,
                Seat: player,
                Plan: true));
        }

        return true;
    }

    private static void FinalizeAllyEntry(World world, Card ally, int player) =>
        world.Agenda.Then(new PhaseStep(
            Steps.FinalizeAllyEntry,
            world.Agenda.Current?.Round ?? 0,
            0,
            Subject: ally.ObjectId,
            Seat: player,
            Plan: true));

    private static void Played(World world, Seat seat, Card card)
    {
        // The card has moved and all state it enters with has been applied.
        // The agenda supplies the ordinary response window, including the
        // optional responses `rr:ability.11` says must be chosen rather than
        // silently resolved or refused.
        world.Agenda.Then(new PhaseStep(
            Steps.CardPlayed,
            world.Agenda.Current?.Round ?? 0,
            0,
            Index: seat.Index,
            Subject: card.ObjectId,
            Seat: seat.Index));
    }

    /// <summary>
    /// A third restricted card forces one out — <c>rr:restricted</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "A player <b>can</b> play or put into play a restricted card even if they
    /// already control two restricted cards. However, if a player ever controls
    /// more than two restricted cards in play, they must <b>immediately</b>
    /// choose and discard from play restricted cards they control until they
    /// have only two."
    /// </para>
    /// <para>
    /// So the limit is not a play restriction — the card goes into play first
    /// and something has to leave afterwards. <c>rr:restricted.1</c> writes it
    /// as a <b>Forced Response</b> for exactly that reason.
    /// </para>
    /// <para>
    /// <b>Which card leaves is the player's choice and this does not ask.</b>
    /// The card just played is kept and the oldest of the others goes, which is
    /// deterministic and stated rather than silently arbitrary. See MARVEL-187,
    /// the same shape of gap.
    /// </para>
    /// </remarks>
    private static void Restricted(
        World world, ICardFacts facts, Seat seat, Card played, List<GameEvent> events)
    {
        if (StateFields.Modified(world, played, "restricted", facts, world.Players) <= 0)
        {
            return;
        }

        var held = new List<Card>();
        foreach (var area in world.Areas.ToList())
        {
            if (!DeckTypes.IsInPlay(area.Type))
            {
                continue;
            }

            held.AddRange(area.Cards.Where(card =>
                card.Owner == seat.Index
                && StateFields.Modified(world, card, "restricted", facts, world.Players) > 0));
        }

        // `StateFields.RestrictedLimit` is the two the rule names.
        foreach (var card in held
            .Where(card => card.ObjectId != played.ObjectId)
            .OrderBy(card => card.ObjectId)
            .Take(Math.Max(0, held.Count - (int)StateFields.RestrictedLimit)))
        {
            Discard.Card(world, card, "restricted", events);
        }
    }
}
