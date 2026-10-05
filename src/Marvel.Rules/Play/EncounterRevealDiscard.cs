using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Discards a revealed treachery after its initiated effects finish.</summary>
internal static class EncounterRevealDiscard
{
    internal static void Schedule(
        World world, ICardFacts facts, Card card, int player, int round,
        Occurrence revealOccurrence, bool beforeResponses = true)
    {

        // Step 4. "If the card is a treachery, discard it." This is agenda
        // work rather than an inline move because `rr:treachery.2.1` keeps a
        // treachery whose last effect initiates activations faceup until all of
        // them finish. `Then` places this behind any activations or choices the
        // When Revealed text just scheduled.
        if (EffectiveCards.Kind(card, facts) == CardKind.Treachery)
        {
            world.Agenda.Then(new PhaseStep(
                Steps.DiscardRevealedTreachery,
                round,
                4,
                Subject: card.ObjectId,
                Seat: player,
                Plan: true));

            // Reveal responses wait for all four reveal steps. Move both the
            // work initiated by the final effect and this discard continuation
            // ahead of that response window, preserving their scheduled order.
            if (beforeResponses)
            {
                world.Agenda.BeforeResponses(revealOccurrence);
            }
        }
    }

    internal static void Resolve(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var card = world.Cards[step.Subject];
        // The area check keeps an ability that moved the treachery from being
        // undone. The kind check makes a reconstructed agenda refuse stale or
        // malformed continuation data rather than discarding another type.
        if (EffectiveCards.Kind(card, facts) != CardKind.Treachery
            || card.Area.Type != DeckType.RevealingArea)
        {
            return;
        }

        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        var from = card.Area;
        World.MoveToTop(card, discard);
        events.Add(new CardsMoved(
            Places.Reference(from),
            Places.Reference(discard),
            [new Landing(card.ObjectId, discard.Cards.Count - 1)])
        {
            Trigger = "villain phase",
            Verb = "Reveal",
        });
    }

}
