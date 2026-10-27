using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Assesses and commits cancellation of one exact pending occurrence.</summary>
internal static class AbilityOccurrenceCancellation
{
    internal static bool CanResolve(World world, Occurrence occurrence)
    {
        if (world.Agenda.IsOutstanding(occurrence)) return true;
        // Engine choice: id zero is the read-only candidate used before opening
        // the paid event window. It is never committed as an agenda occurrence.
        // rr:initiating-abilities.step.5: "Pay the cost(s)."
        // rr:initiating-abilities.step.6: "The card commences being played".
        return occurrence.Id == 0 && occurrence.Is(EventPlayTiming.Initiated)
            && occurrence.Subject >= 0 && occurrence.Subject < world.Cards.Count
            && world.Cards[occurrence.Subject] is var card
            && card.Area.Type == DeckType.RevealingArea
            && card.Owner == occurrence.Player
            && world.Facts.Kind(card.FaceId) == CardKind.Event;
    }

    internal static bool Cancel(World world, Occurrence occurrence)
    {
        // rr:event.2: "Only the effects are canceled." The event's paid costs
        // and subsequent explicit discard belong to their existing procedures.
        // rr:then.2: "If the pre-”then” text of an effect does not fully resolve,
        // the post-”then” text does not attempt to resolve."
        if (!world.Agenda.IsOutstanding(occurrence)) return false;
        world.Agenda.Cancel(occurrence);
        return true;
    }
}
