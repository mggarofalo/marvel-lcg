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
            if (seat.Deck.Cards.Count == 0)
            {
                // `rr:player-deck.3` -- a player who would draw from an empty
                // deck shuffles their discard pile into a new one, and that
                // shuffle consumes randomness. Doing it at the wrong moment
                // changes every card drawn for the rest of the game, so it is
                // named rather than approximated.
                throw new RulesNotImplementedException(
                    $"{seat.Name} would draw from an empty deck, which needs the discard "
                    + "pile shuffled into a new one; that is not implemented");
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
        }
    }
}
