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
    /// <summary>Puts a card in its owner's discard pile.</summary>
    /// <param name="world">The board.</param>
    /// <param name="card">The card being discarded.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what moved.</param>
    public static void Card(World world, State.Card card, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        var pile = card.Owner < 0
            ? world.AreaOf(DeckType.EncounterDiscardPile)
            : world.AreaOf(DeckType.DiscardPile, PlayArea.Of(card.Owner), cardOwner: card.Owner);

        var from = card.Area;
        int host = from.Host;
        World.MoveToTop(card, pile);

        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(pile),
            [new Landing(card.ObjectId, pile.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Discard",
        });

        if (host >= 0)
        {
            // A card that leaves play stops being attached, and a client that
            // drew it hanging off the villain has to be told to take it away.
            events.Add(new CardDetached(card.ObjectId, host)
            {
                Trigger = trigger, Verb = "Discard",
            });
        }
    }
}
