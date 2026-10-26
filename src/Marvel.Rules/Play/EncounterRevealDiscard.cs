using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Discards revealed treacheries and attachments unable to enter play.</summary>
internal static class EncounterRevealDiscard
{
    internal static void Schedule(
        World world, ICardFacts facts, Card card, int player, PhaseStep owner,
        Occurrence revealOccurrence, bool beforeResponses = true)
    {

        // Step 4. "If the card is a treachery, discard it." This is agenda
        // work rather than an inline move because `rr:treachery.2.1` keeps a
        // treachery whose last effect initiates activations faceup until all of
        // them finish. `Then` places this behind any activations or choices the
        // When Revealed text just scheduled.
        // rr:attach-to.3: "If such a card cannot remain in its prior state or
        // game area, discard it." An attachment whose placement failed stays
        // staged through its When Revealed effects and leaves before responses.
        if (EffectiveCards.Kind(card, facts) == CardKind.Treachery
            || (EffectiveCards.Kind(card, facts) == CardKind.Attachment
                && card.Area.Type == DeckType.RevealingArea
                && world.PlacementAbilities.AttachmentTargets(world, card, player) is not null))
        {
            world.Agenda.Then(new PhaseStep(
                new RevealStageOperation(RevealStageKind.DiscardRevealedTreachery),
                owner.Round,
                4,
                Subject: card.ObjectId,
                Seat: player,
                Plan: true
            ));

            // Reveal responses wait for all four reveal steps. Move both the
            // work initiated by the final effect and this discard continuation
            // ahead of that response window, preserving their scheduled order.
            if (beforeResponses)
            {
                world.Agenda.BeforeResponses(owner, revealOccurrence);
            }
        }
    }

    internal static void Resolve(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var card = world.Cards[step.Subject];
        // Cards that entered play or moved elsewhere during their text are
        // outside this cleanup. The scheduled operation spelling is an engine
        // ledger choice shared by both revealed-card cleanup cases.
        if (card.Area.Type != DeckType.RevealingArea
            || (EffectiveCards.Kind(card, facts) != CardKind.Treachery
                && (EffectiveCards.Kind(card, facts) != CardKind.Attachment
                    || world.PlacementAbilities.AttachmentTargets(world, card, step.Seat) is null)))
        {
            return;
        }

        var discard = world.EncounterTopology is { } topology
            ? topology.ForCard(card).Discard
            : world.AreaOf(DeckType.EncounterDiscardPile);
        var from = card.Area;
        World.MoveToTop(card, discard);
        events.Add(new CardsMoved(
            Places.Reference(from),
            Places.Reference(discard),
            [new Landing(card.ObjectId, discard.Cards.Count - 1)])
        {
            Trigger = "villain phase",
            Verb = "Reveal",
            SubjectFaces = OccurrenceFaces.For(card),
        });
    }

}
