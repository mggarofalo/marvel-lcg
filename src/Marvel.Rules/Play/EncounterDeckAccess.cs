using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Draws and immediately resets one explicitly paired encounter deck.</summary>
internal static class EncounterDeckAccess
{
    internal static Card? TakeTop(World world, Area deck, Area? pile, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);

        if (deck.Cards.Count == 0 && !Reset(world, deck, pile, trigger, events))
        {
            return null;
        }

        var card = deck.TakeTop();
        if (deck.Cards.Count == 0)
        {
            Reset(world, deck, pile, trigger, events);
        }

        return card;
    }

    internal static bool Reset(World world, Area deck, Area? pile, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);
        pile ??= EncounterRouting.Discard(world);

        if (deck.Cards.Count > 0)
        {
            return false;
        }

        if (pile.Cards.Count == 0)
        {
            world.Finish(Outcome.PlayersLose, PlayerLossReason.EncounterCardsExhausted);
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
            Trigger = trigger,
            Verb = "Reset",
        });

        world.Shuffle(deck);
        events.Add(new AreaReordered(
            Places.Reference(deck), [.. deck.Cards.Select(card => card.ObjectId)])
        {
            Trigger = trigger,
            Verb = "Shuffle",
        });

        EncounterDeck.PlaceAccelerationToken(world, trigger, events);

        return true;
    }
}
