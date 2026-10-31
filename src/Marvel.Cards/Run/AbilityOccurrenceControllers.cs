using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>The controller of a triggered ability fixed by the kind of its occurrence, if any.</summary>
internal static class AbilityOccurrenceControllers
{
    internal static int? Of(World world, Card card, Occurrence occurrence)
    {
        // A spent resource card answers for the player who spent it.
        if (occurrence.Is(ResourceSpentWindow.Condition))
            return occurrence.SpentResourceCards.FirstOrDefault(spent => spent.Card == card.ObjectId)?.Player;
        // A response to a ready is used by its own controller; its "you" names the
        // controller of the card the instruction readied (ReadiedTriggerMatching).
        return occurrence.Is(CardReadying.Readied) ? AbilityCardQueries.ControllerOf(world, card) : null;
    }
}
