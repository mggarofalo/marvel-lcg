using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>
/// The encounter deck running out — <c>rr:encounter-deck</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:encounter-deck.1</c>: "If the encounter deck is empty, the encounter
/// discard pile is <b>immediately</b> shuffled to create a new encounter deck.
/// When this occurs, place an acceleration token next to the main scheme deck."
/// </para>
/// <para>
/// The player's deck has the same shape (<see cref="PlayerDeck"/>) and a
/// different price: that one costs an encounter card, this one costs an
/// acceleration token, permanently. Both are reasons a long game gets harder
/// rather than settling into a loop.
/// </para>
/// </remarks>
public static class EncounterDeck
{
    /// <summary>
    /// The digest's key for acceleration tokens on a card.
    /// </summary>
    /// <remarks>
    /// <b>The <c>k_</c> prefix is the measured convention; this particular name
    /// is not measured.</b> The recorded board carries <c>k_threat</c> and
    /// <c>k_first_player_token</c> and no third token pool, because nothing in
    /// that game places one. Nothing registers this key, so it stays off the
    /// wire — <c>StateFields.TokensOnceInPlay</c> decides what a card emits, and
    /// this is not in it. The count is tracked; where it belongs on the wire
    /// waits for a recording that has one.
    /// </remarks>
    public const string AccelerationToken = "k_acceleration";

    /// <summary>
    /// Takes the top card, resetting the deck first if it is empty.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what moved.</param>
    /// <returns>The card, or null when there was none to be had.</returns>
    public static Card? TakeTop(World world, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);

        var deck = world.AreaOf(DeckType.EncounterDeck);
        if (deck.Cards.Count == 0)
        {
            Reset(world, trigger, events);
        }

        return deck.TakeTop();
    }

    /// <summary>
    /// Shuffles the encounter discard pile into a new encounter deck.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:encounter-deck.4</c> is the other half and it ends the game: "if
    /// there are no cards in both the encounter deck and the encounter discard
    /// pile simultaneously <i>(such as all cards from the encounter deck being
    /// in play)</i>, an infinite loop occurs with an infinite number of
    /// acceleration tokens being placed next to the main scheme deck. <b>If
    /// this happens, the players lose.</b>"
    /// </para>
    /// <para>
    /// So an empty pair is not a quiet no-op the way an empty player deck is —
    /// the rules work through what the loop would do and then name the result.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what moved.</param>
    /// <returns>Whether a new deck was made.</returns>
    public static bool Reset(World world, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);

        var deck = world.AreaOf(DeckType.EncounterDeck);
        var pile = world.AreaOf(DeckType.EncounterDiscardPile);
        if (deck.Cards.Count > 0)
        {
            return false;
        }

        if (pile.Cards.Count == 0)
        {
            world.Finish(Outcome.PlayersLose);
            return false;
        }

        var landings = new List<Landing>();
        while (pile.Cards.Count > 0)
        {
            var card = pile.Cards[0];
            World.MoveToTop(card, deck);
            landings.Add(new Landing(card.ObjectId, deck.Cards.Count - 1));
        }

        events.Add(new CardsMoved(
            Places.Reference(pile), Places.Reference(deck), landings)
        {
            Trigger = trigger, Verb = "Reset",
        });

        world.Shuffle(deck);
        events.Add(new AreaReordered(
            Places.Reference(deck), [.. deck.Cards.Select(card => card.ObjectId)])
        {
            Trigger = trigger, Verb = "Shuffle",
        });

        // The price, and it is permanent: `rr:acceleration-token.2.1` says
        // "acceleration tokens on the main scheme cannot be removed from play",
        // and `rr:main-scheme-main-scheme-deck.5` carries them to the next
        // stage. Every reshuffle makes every later villain phase worse.
        if (world.TheCardIn(DeckType.MainSchemesArea) is { } scheme)
        {
            long before = scheme.Tokens.GetValueOrDefault(AccelerationToken);
            scheme.PlaceTokens(AccelerationToken, 1);
            events.Add(new FieldSet(scheme.ObjectId, AccelerationToken, before, before + 1)
            {
                Trigger = trigger, Verb = "Accelerate",
            });
        }

        return true;
    }
}
