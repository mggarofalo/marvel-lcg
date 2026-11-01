using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>The controller of a triggered ability fixed by the kind of its occurrence, if any.</summary>
internal static class AbilityOccurrenceControllers
{
    internal static int? Of(World world, Card card, Occurrence occurrence, Dsl.AbilityTrigger trigger)
    {
        // A spent resource card answers for the player who spent it.
        if (occurrence.Is(ResourceSpentWindow.Condition))
            return occurrence.SpentResourceCards.FirstOrDefault(spent => spent.Card == card.ObjectId)?.Player;
        // A response to a ready is used by its own controller; its "you" names the
        // controller of the card the instruction readied (ReadiedTriggerMatching).
        // An excess-damage ability is too; its "you" names the identity credited with
        // the damage (AbilityExcessDamageInstances), not the card that dealt it.
        return occurrence.Is(CardReadying.Readied) || AbilityExcessDamageInstances.Applies(trigger, occurrence)
            ? AbilityCardQueries.ControllerOf(world, card) : null;
    }
}
