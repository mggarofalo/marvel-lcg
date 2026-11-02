using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Removes out-of-play player cards from a hand or deck from the game.</summary>
/// <remarks>
/// Printed 16138 Pirate Commander removes "1 card at random in your hand from
/// the game" and 16139 Pirate Lackey "the top card of your deck". A removed card
/// is out of the game, and its owner may look at it (ruling 57fc672ad0921420).
/// The ledger verb is an engine choice shared with in-play removal.
/// </remarks>
public static class PlayerCardRemoval
{
    /// <summary>Remove random cards from a player's hand from the game.</summary>
    /// <returns>The cards removed.</returns>
    public static IReadOnlyList<Card> RandomFromHand(
        World world, int player, long count, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);
        var hand = world.Seats[player].Hand;
        var removed = new List<Card>();
        for (long gone = 0; gone < count && hand.Cards.Count > 0; gone++)
        {
            var card = world.Random.Choice(hand.Cards);
            Remove(world, card, trigger, events);
            removed.Add(card);
        }
        return removed;
    }

    /// <summary>Remove at most the cards in a player's current deck from its top.</summary>
    /// <remarks>
    /// <c>rr:player-deck.1</c>: "If a player deck empties, the player shuffles
    /// their discard pile to make a new deck. That player immediately deals
    /// themself one facedown encounter card." As for a discard from the deck
    /// (<c>rr:player-deck.3</c>), the starting deck bounds the instruction, so
    /// the deck can only empty with its last removal.
    /// </remarks>
    /// <returns>The cards removed.</returns>
    public static IReadOnlyList<Card> TopOfDeck(
        World world, int player, long count, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);
        var deck = world.Seats[player].Deck;
        long remaining = Math.Min(count, deck.Cards.Count);
        var removed = new List<Card>();
        for (long index = 0; index < remaining; index++)
        {
            var card = deck.Cards[^1];
            Remove(world, card, trigger, events);
            removed.Add(card);
        }
        if (removed.Count > 0) PlayerDeck.Reset(world, player, events);
        return removed;
    }

    private static void Remove(World world, Card card, string trigger, List<GameEvent> events)
    {
        var from = card.Area;
        var removed = world.AreaOf(DeckType.RemovedArea);
        World.MoveToTop(card, removed);
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(removed),
            [new Landing(card.ObjectId, removed.Cards.Count - 1)])
        { Trigger = trigger, Verb = "Remove_From_Game" });
    }
}
