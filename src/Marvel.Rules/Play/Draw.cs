using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>
/// Drawing cards — <c>rr:draw-drawing-cards</c>.
/// </summary>
/// <remarks>
/// "If a player is instructed to draw one or more cards, those cards are drawn
/// from the top of their deck one at a time", and <c>.1</c> puts them in that
/// player's hand. One at a time is not decoration: a deck that empties part-way
/// through has to be rebuilt before the next card comes off it
/// (<c>rr:player-deck</c>), so drawing two is not the same as taking two.
/// </remarks>
public static class Draw
{
    /// <summary>Draws cards from a player's deck into their hand.</summary>
    /// <param name="world">The board.</param>
    /// <param name="player">Whose deck.</param>
    /// <param name="count">How many.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what moved.</param>
    public static void Cards(
        World world, int player, int count, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);

        var seat = world.Seats[player];
        for (int drawn = 0; drawn < count; drawn++)
        {
            // `rr:player-deck.4` -- a reset owed from earlier, because the
            // discard pile was empty when the deck ran out and has since
            // gained a card.
            PlayerDeck.Reset(world, player, events);

            if (seat.Deck.Cards.Count == 0)
            {
                // Deck and discard pile both empty. `rr:player-deck.4` makes
                // that a legal board rather than a fault: there is no card to
                // draw, so no card is drawn.
                return;
            }

            var card = seat.Deck.Cards[^1];
            var from = card.Area;
            World.MoveToTop(card, seat.Hand);
            events.Add(new CardsMoved(
                Places.Reference(from), Places.Reference(seat.Hand),
                [new Landing(card.ObjectId, seat.Hand.Cards.Count - 1)])
            {
                Trigger = trigger, Verb = "Draw",
            });

            // `rr:player-deck.1`'s trigger is the deck *emptying*, not the next
            // attempt to draw from it, and `rr:player-deck.2` says the player
            // "continues to draw cards up to the specified number" across the
            // reshuffle. Both need this here rather than at the top of the
            // loop: the shuffle draws from the game's one random stream, so
            // moving it one draw later changes every card that follows.
            PlayerDeck.Reset(world, player, events);
        }
    }
}
