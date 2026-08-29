using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>
/// Dealing an encounter card — <c>rr:deal-deal-an-encounter-card</c>.
/// </summary>
/// <remarks>
/// <para>
/// "The player takes the top card of the encounter deck and places it
/// <b>facedown</b> in front of them. This card is <b>not revealed at this
/// time</b>. This card is added to the <b>queue</b> of cards that player
/// resolves during the villain phase."
/// </para>
/// <para>
/// All three emphases are load-bearing, and together they are why this is a
/// primitive rather than something the villain phase does inline. A card
/// ability can deal a card, and so can <c>rr:player-deck.1</c> when a player's
/// deck runs out in the middle of the player phase — a whole phase before the
/// step that reveals it. Dealing and revealing are separated by an arbitrary
/// stretch of game, so they cannot be one call.
/// </para>
/// </remarks>
public static class Deal
{
    /// <summary>
    /// Deals one facedown encounter card to a player's queue.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="player">Who is dealt to.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what moved.</param>
    /// <returns>The card, or null when the encounter deck is empty.</returns>
    public static Card? EncounterCard(
        World world, int player, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);

        var deck = world.AreaOf(DeckType.EncounterDeck);

        // `rr:encounter-deck.3`: "if the encounter deck empties during the
        // resolution of any other type of game effect *(for example, the
        // dealing of encounter cards)*, that effect finishes resolving after
        // the encounter deck has been reset." So the deal does not stop at an
        // empty deck -- it waits for the reset and carries on.
        var card = EncounterDeck.TakeTop(world, trigger, events);
        if (card is null)
        {
            return null;
        }

        // Dealt to the player, so it sits in their play area until it is
        // revealed. The recorded digest never catches a card here -- the whole
        // deal-and-reveal happens between two decisions -- but the engine's own
        // log shows the intermediate pile, and skipping it would make a
        // two-player board deal in the wrong order.
        var queue = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player));
        queue.Append(card);

        events.Add(new CardsMoved(
            Places.Reference(deck), Places.Reference(queue),
            [new Landing(card.ObjectId, queue.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Deal",
        });

        return card;
    }

    /// <summary>Deals one already identified encounter card to a player's queue.</summary>
    /// <remarks>
    /// Some card text says to deal “that card” after moving it elsewhere. The
    /// identity of the card is part of the instruction, so drawing the current
    /// top card would silently substitute a different game element.
    /// </remarks>
    public static void EncounterCard(
        World world, Card card, int player, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        var from = card.Area;
        var queue = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player));
        World.MoveToTop(card, queue);
        card.TurnFaceDown();
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(queue),
            [new Landing(card.ObjectId, queue.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Deal",
        });
    }

    /// <summary>
    /// The next card waiting to be revealed, and whose it is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:villain-phase.step.4</c>: "The first player reveals each of their
    /// encounter cards, one card at a time <b>in the order in which they were
    /// dealt</b>, resolving each card based on its card type. Each player
    /// repeats this process in player order, <b>until no dealt encounter cards
    /// remain</b>."
    /// </para>
    /// <para>
    /// That last clause is a loop and not a list, which is what
    /// <c>rr:deal-deal-an-encounter-card.1</c> needs: "if a player is dealt an
    /// encounter card during step three or four of the villain phase, the extra
    /// encounter card is added to the queue of cards that are being dealt and
    /// revealed in <b>those same steps</b>." A card revealed in step 4 that
    /// deals another card has that card revealed in the same step 4 — so the
    /// step asks this again after every reveal rather than being handed a list
    /// at the start.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    public static (Card Card, int Player)? NextToReveal(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        foreach (int player in world.PlayerOrder)
        {
            var queue = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player));

            // The bottom of the pile, because `Area.Append` puts each dealt
            // card on top and the rule asks for the order they were dealt in.
            if (queue.Cards.Count > 0)
            {
                return (queue.Cards[0], player);
            }
        }

        return null;
    }

    /// <summary>
    /// How many extra cards the hazard icons in play are worth —
    /// <c>rr:hazard-icon</c>.
    /// </summary>
    /// <remarks>
    /// "During the Deal Encounter Cards step of the villain phase, for each
    /// hazard icon on cards in play, deal <b>one player</b> one additional card
    /// <i>(not one card per player)</i>." So this counts icons, not cards and
    /// not seats, and the caller deals them round the table one at a time.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    public static long HazardIcons(World world, ICardFacts facts)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);

        long icons = 0;
        foreach (var area in world.Areas)
        {
            if (!DeckTypes.IsInPlay(area.Type))
            {
                continue;
            }

            foreach (var card in area.Cards)
            {
                // `Modified` and not the printed value: `rr:hazard-icon.1`
                // makes each icon a constant ability, and `rr:modifiers` has
                // the game re-check a modified quantity constantly, so an
                // effect adding a hazard icon counts here too.
                icons += StateFields.Modified(world, card, "hazard", facts, world.Players);
            }
        }

        return icons;
    }
}
