using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>
/// Discarding a card — <c>rr:discard</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:discard-pile</c>: a discarded card goes to <b>its owner's</b> discard
/// pile, and an encounter card's owner is the scenario. That is why this reads
/// the card's owner rather than being told where to put it: a caller that had to
/// choose could choose wrong, and a player card in the encounter discard is a
/// board nothing else would notice.
/// </para>
/// <para>
/// A rules primitive rather than something a card does for itself, for the same
/// reason <see cref="Draw"/> is one: it is a rule of the game that happens to be
/// reachable from card text.
/// </para>
/// </remarks>
public static class Discard
{
    /// <summary>
    /// Puts a card in its owner's discard pile, or removes a spent status component.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="card">The card being discarded.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what moved.</param>
    public static void Card(World world, State.Card card, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        // `rr:attach-to.1`: "if the game element an attachment is attached to
        // leaves play, the attachment is discarded." Snapshot the areas because
        // discarding an attachment moves it and can itself detach hosted cards.
        if (DeckTypes.IsInPlay(card.Area.Type))
        {
            var attachedCards = world.Areas
                .Where(area => area.Host == card.ObjectId)
                .SelectMany(area => area.Cards)
                .ToList();
            if (attachedCards.FirstOrDefault(attached => world.Facts.PrintedValue(
                    attached.FaceId, "Permanent", world.Players) > 0) is { } permanent)
            {
                // `rr:permanent.5` resolves the attachment's attach-to text
                // again and removes it only if no valid target exists. The
                // ordinary discard rule is deliberately not guessed here.
                throw new RulesNotImplementedException(
                    $"permanent attachment {permanent.ObjectId} lost host "
                    + $"{card.ObjectId}, and rr:permanent.5 is not implemented");
            }

            foreach (var attached in attachedCards)
            {
                Card(world, attached, trigger, events);
            }
        }

        // A status card is discarded by its keyword rule, but it is not an
        // encounter card and therefore has no encounter discard pile. The
        // engine chooses RemovedArea as the out-of-play home for spent status
        // components; the Rules Reference does not name a separate status pile.
        var pile = card.Area.Type == DeckType.StatusArea
            ? world.AreaOf(DeckType.RemovedArea)
            : card.Owner < 0
                ? world.AreaOf(DeckType.EncounterDiscardPile)
                : world.AreaOf(
                    DeckType.DiscardPile, PlayArea.Of(card.Owner), cardOwner: card.Owner);

        var from = card.Area;
        int host = from.Host;
        World.MoveToTop(card, pile);

        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(pile),
            [new Landing(card.ObjectId, pile.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Discard",
        });

        // `rr:player-deck.4`: a deck that emptied beside an empty discard pile
        // "does not reset until there is at least one card in the player's
        // discard pile, **then** the player deals themself one facedown
        // encounter card". This is *then* -- a card has just landed there.
        if (card.Owner >= 0)
        {
            PlayerDeck.Reset(world, card.Owner, events);
        }

        if (host >= 0)
        {
            // A card that leaves play stops being attached, and a client that
            // drew it hanging off the villain has to be told to take it away.
            events.Add(new CardDetached(card.ObjectId, host)
            {
                Trigger = trigger, Verb = "Discard",
            });
        }

        // A support such as The Triskelion can leave play and reduce a
        // player's modified ally limit. `rr:ally-limit` applies whenever the
        // count is over the live limit, not only when the latest ally entered.
        foreach (int player in world.PlayerOrder)
        {
            CardPlay.CheckAllyLimit(world, world.Facts, player);
        }
    }
}
