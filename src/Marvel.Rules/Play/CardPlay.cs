using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Rules.Play.CardPayment;
using static Marvel.Rules.Play.CardPlayLegality;
using static Marvel.Rules.Play.CardControlTransfer;
using static Marvel.Rules.Play.CardEntry;

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
    /// <inheritdoc cref="CardPayment.ReduceNextCardCost"/>
    public static ContinuousEffects.Registration ReduceNextCardCost(
        World world, Card source, int player, long amount) =>
        CardPayment.ReduceNextCardCost(world, source, player, amount);

    /// <inheritdoc cref="CardPayment.CostOf"/>
    public static AdjustedCardCost CostOf(
        World world, ICardFacts facts, Seat seat, Card card) =>
        CardPayment.CostOf(world, facts, seat, card);

    /// <inheritdoc cref="CardPayment.UseCostModifiers"/>
    public static void UseCostModifiers(World world, AdjustedCardCost cost) =>
        CardPayment.UseCostModifiers(world, cost);

    /// <summary>Returns every resource generator that may pay for a card.</summary>
    public static IReadOnlyList<ResourceSource> Generators(
        World world, ICardFacts facts, Seat seat, Card? payingFor = null) =>
        CardPayment.Generators(world, facts, seat, payingFor);

    /// <summary>Returns generators supplied by an explicit card capability.</summary>
    public static IReadOnlyList<ResourceSource> Generators(
        World world, ICardFacts facts, Seat seat,
        IResourceCardAbilities resourceAbilities, Card? payingFor = null) =>
        CardPayment.Generators(world, facts, seat, resourceAbilities, payingFor);

    /// <inheritdoc cref="CardPayment.Paying"/>
    public static IReadOnlyList<Seat> Paying(
        World world, ICardFacts facts, Seat seat, Card card) =>
        CardPayment.Paying(world, facts, seat, card);

    /// <inheritdoc cref="CardPayment.Price"/>
    public static CostOption? Price(World world, ICardFacts facts, Seat seat, Card card) =>
        CardPayment.Price(world, facts, seat, card);

    /// <inheritdoc cref="CardPlayLegality.WithinPerPlayerLimit"/>
    public static bool WithinPerPlayerLimit(
        World world, ICardFacts facts, Seat seat, Card card, IReadOnlyList<int>? targets,
        ICardPlayAbilities abilities) =>
        CardPlayLegality.WithinPerPlayerLimit(world, facts, seat, card, targets, abilities);

    /// <inheritdoc cref="CardPlayLegality.LegalAttachmentTargets"/>
    public static IReadOnlyList<int>? LegalAttachmentTargets(
        World world, ICardFacts facts, Seat seat, Card card, ICardPlayAbilities abilities) =>
        CardPlayLegality.LegalAttachmentTargets(world, facts, seat, card, abilities);

    /// <inheritdoc cref="CardControlTransfer.TakeControl"/>
    public static void TakeControl(
        World world, ICardFacts facts, Card card, int player) =>
        CardControlTransfer.TakeControl(world, facts, card, player);

    /// <inheritdoc cref="CardControlTransfer.ReturnToOwnerControl"/>
    public static void ReturnToOwnerControl(World world, ICardFacts facts, Card card) =>
        CardControlTransfer.ReturnToOwnerControl(world, facts, card);

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
        World world, ICardFacts facts, ICardPlayAbilities abilities, Seat seat, Card card,
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
        World world, ICardFacts facts, ICardPlayAbilities abilities, Seat seat, Card card,
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
        World world, ICardFacts facts, ICardPlayAbilities abilities, Seat seat, Card card,
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
    /// play area. Ordinarily the card's <see cref="Card.Owner"/> does not
    /// change; if it later leaves play, <c>rr:ownership-and-control.7.2</c>
    /// sends it to its owner's corresponding out-of-play area. The scenario
    /// player-card exception in <c>rr:ownership-and-control.2.2</c> instead
    /// makes its entering controller its owner.
    /// </para>
    /// <para>
    /// <c>rr:play-put-into-play.3</c> says this is not playing the card, so no
    /// <c>CardPlayed</c> step is scheduled. It still enters play, and therefore
    /// uses the same entry lifecycle as a card played from hand.
    /// </para>
    /// </remarks>
    public static void PutAllyIntoPlay(
        World world, ICardFacts facts, ICardPlayAbilities abilities, Card ally,
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

        if (facts.RequiredForm(ally.FaceId) is { } form
            && !Forms.In(world, world.Seats[controller], facts, form))
        {
            throw new RulesNotImplementedException(
                $"ally {ally.ObjectId} can only be put into play in {form} form");
        }

        if (Uniqueness.IsBlocked(world, facts, ally, PlayArea.Of(controller)))
        {
            // `rr:unique-icon.4.1`: an attempted put-into-play has no effect.
            return;
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
            Trigger = trigger,
            Verb = "Put_Into_Play",
        });

        if (previousController != controller)
        {
            events.Add(new ControlChanged(ally.ObjectId, previousController, controller)
            {
                Trigger = trigger,
                Verb = "Put_Into_Play",
            });
        }

        if (AllyLimit(world, facts, world.Seats[controller], ally))
        {
            FinalizeAllyEntry(world, ally, controller);
        }
        else
        {
            Reveal.EnterPlay(world, facts, ally, events, abilities: abilities);
            Entered(world, ally, controller);
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
        => Spend(world, facts, world.ResourceAbilities, hands, paying, cost, required,
            itself, payer, events, payingFor, resourcePayers);

    /// <summary>Spends resources through one explicit card capability.</summary>
    public static void Spend(
        World world, ICardFacts facts, IResourceCardAbilities resourceAbilities,
        IReadOnlyList<Area> hands, IReadOnlyList<int> paying,
        long cost, string required, int itself, int payer, List<GameEvent> events,
        Card? payingFor = null, IReadOnlyDictionary<int, int>? resourcePayers = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(resourceAbilities);
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
            ? resourceAbilities.ResourceAbilities(world, payer)
                .GroupBy(source => source.Effect)
                .ToDictionary(group => group.Key, _ => payer)
            : resourcePayers;

        foreach (int id in paying)
        {
            var source = world.Cards[id];
            if (abilityPayers.TryGetValue(id, out int abilityPayer)
                && !hands.Contains(source.Area))
            {
                generated.Append(resourceAbilities.UseResource(
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
            generated.Append(resourceAbilities.ResourcesGeneratedBy(world, source, payingFor));
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
}
