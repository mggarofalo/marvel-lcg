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
    /// Takes the top card, resetting before an empty take or immediately after
    /// the take empties it into an available discard pile.
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
        if (deck.Cards.Count == 0 && !Reset(world, trigger, events))
        {
            return null;
        }

        var card = deck.TakeTop();
        if (deck.Cards.Count == 0)
        {
            Reset(world, trigger, events);
        }

        return card;
    }

    /// <summary>
    /// Discards from the current encounter deck until a card of the requested
    /// kind and optional printed trait is discarded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:encounter-deck.2</c> gives this discard its boundary: it stops when
    /// the condition is met or when the encounter deck is emptied, and an
    /// empty deck fulfills the effect without continuing through the newly
    /// shuffled deck. The reset is still immediate under
    /// <c>rr:encounter-deck.1</c>; this method performs it, then returns without
    /// taking a card from the replacement deck.
    /// </para>
    /// <para>
    /// The matching card is returned by object identity. It has already been
    /// discarded, and if it emptied the deck it may already have been shuffled
    /// back into the replacement deck. A following instruction such as "put
    /// that minion into play" can therefore move the exact card that satisfied
    /// the condition rather than searching for another copy.
    /// </para>
    /// <para>
    /// The trait is printed because cards in the encounter deck are out of
    /// play. <c>rr:ability</c> leaves constant abilities inactive there, so a
    /// granted in-play trait cannot change which facedown deck card satisfies
    /// this instruction.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">Printed card facts.</param>
    /// <param name="kind">The printed card kind that ends the discard.</param>
    /// <param name="trait">An optional printed trait that must also match.</param>
    /// <param name="trigger">What caused the discard, for the event stream.</param>
    /// <param name="events">Where to record the discard and any reset.</param>
    /// <returns>The exact matching card, or null when the current deck ran out.</returns>
    public static Card? DiscardUntil(
        World world,
        ICardFacts facts,
        CardKind kind,
        string trigger,
        List<GameEvent> events,
        string? trait = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(events);

        var deck = world.AreaOf(DeckType.EncounterDeck);
        if (deck.Cards.Count == 0 && !Reset(world, trigger, events))
        {
            return null;
        }

        int remaining = deck.Cards.Count;
        for (int discarded = 0; discarded < remaining; discarded++)
        {
            var card = deck.TakeTop()
                ?? throw new InvalidOperationException(
                    "the encounter deck changed while one discard effect was resolving");
            Discard.Card(world, card, trigger, events);

            bool matches = facts.Kind(card.FaceId) == kind
                && (trait is null
                    || facts.Traits(card.FaceId).Contains(trait, StringComparer.Ordinal));

            if (deck.Cards.Count == 0)
            {
                Reset(world, trigger, events);
                return matches ? card : null;
            }

            if (matches)
            {
                return card;
            }
        }

        throw new InvalidOperationException(
            "the encounter discard ended without a match or an empty deck");
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

        PlaceAccelerationToken(world, trigger, events);

        return true;
    }

    /// <summary>Places one acceleration token next to the main scheme.</summary>
    /// <remarks>
    /// <c>rr:acceleration-token.2</c> permits card effects to add one, and
    /// <c>.2.1</c> makes a token beside the main scheme permanent across its
    /// stages. Encounter-deck resets use this same operation, so a token from a
    /// card and one from an empty deck have identical state and event spelling.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="trigger">What placed it, for the event stream.</param>
    /// <param name="events">Where to record the placement.</param>
    /// <returns>Whether a main scheme was present to receive it.</returns>
    public static bool PlaceAccelerationToken(
        World world, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);

        if (world.TheCardIn(DeckType.MainSchemesArea) is not { } scheme)
        {
            return false;
        }

        long before = scheme.Tokens.GetValueOrDefault(AccelerationToken);
        scheme.PlaceTokens(AccelerationToken, 1);
        events.Add(new FieldSet(scheme.ObjectId, AccelerationToken, before, before + 1)
        {
            Trigger = trigger, Verb = "Accelerate",
        });
        return true;
    }
}
