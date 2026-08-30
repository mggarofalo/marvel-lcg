using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>
/// A player's deck running out — <c>rr:player-deck</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:player-deck.1</c>: "If a player deck empties, the player shuffles
/// their discard pile to make a new deck. <b>That player immediately deals
/// themself one facedown encounter card from the top of the encounter deck.</b>"
/// </para>
/// <para>
/// The second sentence is the price of the first, and it is why this is not
/// just a shuffle: running out of cards costs an encounter card. Everything
/// here is one rule with four clauses, so it is one type.
/// </para>
/// </remarks>
public static class PlayerDeck
{
    /// <summary>Discards at most the cards in the current player deck.</summary>
    /// <remarks>
    /// <c>rr:player-deck.3</c>: when this effect empties and reshuffles the
    /// deck, it stops; the replacement deck is not part of the same discard.
    /// The starting count is therefore the boundary even though
    /// <see cref="Discard.Card"/> performs the immediate reset.
    /// </remarks>
    public static IReadOnlyList<Card> DiscardTop(
        World world, int player, long count, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);
        if (count <= 0)
        {
            return [];
        }

        var deck = world.Seats[player].Deck;
        long remaining = Math.Min(count, deck.Cards.Count);
        var discarded = new List<Card>();
        for (long index = 0; index < remaining; index++)
        {
            var card = deck.Cards[^1];
            Discard.Card(world, card, trigger, events);
            discarded.Add(card);
        }

        return discarded;
    }

    /// <summary>
    /// Rebuild a player's deck from their discard pile if it is time to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Does nothing unless the deck is empty <b>and</b> the discard pile is
    /// not. <c>rr:player-deck.4</c>: "If a player deck empties and the player
    /// has no cards in their discard pile, the deck does not reset until there
    /// is at least one card in the player's discard pile, <b>then</b> the
    /// player deals themself one facedown encounter card." So an empty deck
    /// beside an empty discard is a legal, stable board — the player simply
    /// draws nothing — and the reset is owed, not skipped.
    /// </para>
    /// <para>
    /// Called from two places for that reason: before a draw, and from
    /// <see cref="Discard"/> when a card lands in a player's pile. The second
    /// is what makes "then" mean then.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="player">Whose deck.</param>
    /// <param name="events">Where to record what moved.</param>
    /// <returns>Whether the deck was rebuilt.</returns>
    public static bool Reset(World world, int player, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);

        var seat = world.Seats[player];
        var pile = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player);
        if (seat.Deck.Cards.Count > 0 || pile.Cards.Count == 0)
        {
            return false;
        }

        // Bottom to top, so the pile empties in a defined order. The shuffle
        // that follows makes the order irrelevant to the outcome, but not to
        // the event stream a client is drawing.
        var landings = new List<Landing>();
        while (pile.Cards.Count > 0)
        {
            var card = pile.Cards[0];
            World.MoveToTop(card, seat.Deck);
            landings.Add(new Landing(card.ObjectId, seat.Deck.Cards.Count - 1));
        }

        events.Add(new CardsMoved(
            Places.Reference(pile), Places.Reference(seat.Deck), landings)
        {
            Trigger = "player deck empty", Verb = "Reset",
        });

        // Drawn from the game's one stream, so *when* this happens is part of
        // the wire format -- every card either player draws afterwards depends
        // on it. That is why `rr:player-deck.1`'s trigger is the deck emptying
        // and not the next attempt to draw from it.
        world.Shuffle(seat.Deck);
        events.Add(new AreaReordered(
            Places.Reference(seat.Deck),
            [.. seat.Deck.Cards.Select(card => card.ObjectId)])
        {
            Trigger = "player deck empty", Verb = "Shuffle",
        });

        // The price. `rr:deal-deal-an-encounter-card` puts it facedown in the
        // player's queue, to be revealed in the next villain phase -- which is
        // why step 4 had to become a queue before this could be written.
        Deal.EncounterCard(world, player, "player deck empty", events);
        return true;
    }
}
