using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Places an accepted played card faceup and outside play during its initiation.</summary>
internal static class PlayedCardExposure
{
    internal static void Expose(World world, Card card, int player, List<GameEvent> events)
    {
        // rr:initiating-abilities.step.1: "places that card faceup on the table ... (This card is not in play.)"
        var from = card.Area;
        var resolving = world.AreaOf(DeckType.RevealingArea, PlayArea.Of(player), cardOwner: card.Owner);
        if (from == resolving) return;
        World.MoveToTop(card, resolving);
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(resolving),
            [new Landing(card.ObjectId, resolving.Cards.Count - 1)])
        { Trigger = CardPlay.Verb, Verb = CardPlay.Verb });
    }
}
