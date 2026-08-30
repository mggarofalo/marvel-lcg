using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Cards tucked under another card — <c>rr:tuck</c>.</summary>
public static class Tuck
{
    /// <summary>Places one card faceup, out of play, under an in-play host.</summary>
    public static void Card(
        World world, State.Card card, State.Card host,
        string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(events);
        if (!DeckTypes.IsInPlay(host.Area.Type) || ReferenceEquals(card, host))
        {
            throw new RulesNotImplementedException("a tucked card needs a different in-play host");
        }

        var from = card.Area;
        var under = world.AreaOf(
            DeckType.AsideDeck, host.Area.PlayArea, host.ObjectId, card.Owner);
        World.MoveToTop(card, under);
        card.TurnFaceUp();
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(under),
            [new Landing(card.ObjectId, under.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Tuck",
        });
    }
}
