using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

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
    /// Spelled as the recording spells it: <c>datasets/digest/prompts.json</c>
    /// records eighteen <c>Play</c> affordances across its prompts, which is the
    /// most common verb in the fixture.
    /// </remarks>
    public const string Verb = "Play";

    /// <summary>
    /// Every card in a hand that can be spent for resources.
    /// </summary>
    /// <remarks>
    /// <c>rr:resource.1</c> — "discarding cards from their hand to generate the
    /// resource or resources indicated at the bottom-left corner of the card".
    /// A card printing nothing there generates nothing and is not a generator.
    /// <para>
    /// <c>rr:resource-ability</c>'s "<b>Resource</b>" abilities are the other
    /// source and are not implemented; a card whose only contribution is one is
    /// simply absent from this list rather than misreported.
    /// </para>
    /// </remarks>
    /// <param name="facts">The printed card data.</param>
    /// <param name="seat">Whose hand.</param>
    public static IReadOnlyList<ResourceSource> Generators(ICardFacts facts, Seat seat)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(seat);

        var sources = new List<ResourceSource>();
        foreach (var card in seat.Hand.Cards)
        {
            string generates = Resources.GeneratedBy(card.FaceId, facts);
            if (generates.Length > 0)
            {
                sources.Add(new ResourceSource(card.ObjectId, generates));
            }
        }

        return sources;
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

        if (!Permitted(world, facts, seat, card))
        {
            return null;
        }

        // **Events are not a turn option on their own.** `rr:player-turn.5`
        // reaches them through "trigger an **Action** ability on an event card
        // in their hand", so an event without one is played in a window and not
        // here -- 555 of the 602 events in the pool have no Action ability at
        // all. Of the 47 that do, none has its ability authored yet, so
        // offering one would be an affordance that throws when taken.
        //
        // The recorded board is the check: its opening hand holds `01003`
        // Backflip, whose ability is an **Interrupt (defense)**, and the
        // recording does not offer it.
        if (facts.Kind(card.FaceId) == CardKind.Event)
        {
            return null;
        }

        long cost = Resources.Cost(card.FaceId, facts) ?? 0;

        // The card being played cannot also pay for itself: `rr:cost.3` spends
        // resources "by discarding cards from their hand", and this one is
        // leaving the hand to be played.
        var sources = Generators(facts, seat)
            .Where(source => source.Effect != card.ObjectId)
            .ToList();

        long available = sources.Sum(source => (long)source.Generates.Length);
        return available < cost
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
    /// <exception cref="RulesNotImplementedException">
    /// A restriction is not met, the payment does not cover the cost, or the
    /// card needs a rule this engine does not have.
    /// </exception>
    public static void Play(
        World world, ICardFacts facts, ICardAbilities abilities, Seat seat, Card card,
        IReadOnlyList<int> paying, List<GameEvent> events)
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
        if (!Permitted(world, facts, seat, card))
        {
            throw new RulesNotImplementedException(
                $"card {card.ObjectId} cannot be played by {seat.Name} right now");
        }

        // Steps 3 and 4. Determine the cost, with modifiers. Nothing modifies a
        // cost yet, so this is the printed number.
        long cost = Resources.Cost(card.FaceId, facts) ?? 0;

        // Step 5. Pay it -- "if this step is reached and the cost(s) cannot be
        // paid, **abort this process without paying any costs**", so the whole
        // payment is checked before a single card is discarded.
        var spent = new List<Card>();
        var generated = new System.Text.StringBuilder();
        foreach (int id in paying)
        {
            var source = world.Cards[id];
            if (source.Area != seat.Hand)
            {
                throw new RulesNotImplementedException(
                    $"card {id} is not in {seat.Name}'s hand and cannot be spent from it");
            }

            if (source.ObjectId == card.ObjectId)
            {
                throw new RulesNotImplementedException(
                    $"card {id} is being played and cannot also pay for itself");
            }

            spent.Add(source);
            generated.Append(Resources.GeneratedBy(source.FaceId, facts));
        }

        if (!Resources.Pays(generated.ToString(), cost))
        {
            throw new RulesNotImplementedException(
                $"card {card.ObjectId} costs {cost} and the payment generates "
                + $"{generated.Length}; rr:initiating-abilities.step.5 aborts without paying");
        }

        foreach (var source in spent)
        {
            Discard.Card(world, source, Verb, events);
        }

        // Steps 6 and 7. The card is played: it enters play, or it is an event
        // and its ability resolves before it is discarded.
        Enter(world, facts, abilities, seat, card, events);
    }

    /// <summary>
    /// <c>rr:initiating-abilities.step.2</c> — the play restrictions.
    /// </summary>
    /// <remarks>
    /// Only the ones the rules state generally. A card's own restrictions are
    /// its text, which is <c>src/Marvel.Cards</c>'s business, and a card that
    /// has one is not filtered here — it fails when its ability runs.
    /// </remarks>
    private static bool Permitted(World world, ICardFacts facts, Seat seat, Card card)
    {
        if (card.Area != seat.Hand)
        {
            // "Cards are played from a player's hand."
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

    /// <summary>Where a played card goes — <c>rr:enters-play</c>.</summary>
    private static void Enter(
        World world, ICardFacts facts, ICardAbilities abilities, Seat seat, Card card,
        List<GameEvent> events)
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

        var into = kind switch
        {
            CardKind.Ally => world.AreaOf(
                DeckType.AlliesArea, PlayArea.Of(seat.Index), cardOwner: seat.Index),
            CardKind.Support => world.AreaOf(
                DeckType.SupportsArea, PlayArea.Of(seat.Index), cardOwner: seat.Index),

            // `rr:upgrade`: an upgrade attaches to a card, and with no ability
            // saying otherwise that card is the identity playing it.
            CardKind.Upgrade => world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(seat.Index),
                host: seat.IdentityCard.ObjectId, cardOwner: seat.Index),
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
            events.Add(new CardAttached(card.ObjectId, seat.IdentityCard.ObjectId)
            {
                Trigger = Verb, Verb = Verb,
            });
        }

        // `rr:enters-play`: the keywords that fire when a card enters play do
        // not care how it got there. Eighteen allies in the pool print
        // `rr:toughness`, and before this a played one got no tough status card
        // -- only a *revealed* card ran them.
        Reveal.EnterPlay(world, facts, card, events);
        Restricted(world, facts, seat, card, events);
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
