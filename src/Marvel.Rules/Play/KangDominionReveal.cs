using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Reveals stored physical Dominions through the ordinary encounter windows.</summary>
internal static class KangDominionReveal
{
    internal static void Apply(World world, PhaseStep step, KangCentralOperation operation, List<GameEvent> events)
    {
        var host = world.Cards[step.Subject];
        // The engine chooses storage insertion order; printed 4A imposes no order
        // between identical physical Dominions. Keep that order across suspension.
        var cards = operation.Stored ?? BeneathCards.Of(world, host).Select(card => OccurrenceCard.Capture(card, world.Facts)).ToArray();
        if (operation.Index == cards.Count) return;
        var captured = cards[operation.Index];
        var card = world.Cards[captured.Card];
        RequireOriginalStorage(host, card, captured);
        var from = card.Area;
        var origin = new EncounterRevealOrigin(card.ObjectId, Places.Reference(from));
        EncounterRevealExposure.Expose(world, card, "11013a", events);
        var staging = world.AreaOf(DeckType.RevealingArea, PlayArea.Of(step.Seat));
        World.MoveToTop(card, staging);
        events.Add(new CardDetached(card.ObjectId, host.ObjectId) { Trigger = "11013a", Verb = "Reveal" });
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(staging),
            [new Landing(card.ObjectId, staging.Cards.Count - 1)]) { Trigger = "11013a", Verb = "Reveal" });
        world.Agenda.Now([
            new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard, origin), step.Round, step.Number,
                Subject: card.ObjectId, Seat: step.Seat),
            step with { Operation = operation with { Index = operation.Index + 1, Stored = cards } },
        ]);
    }

    private static void RequireOriginalStorage(Card host, Card card, OccurrenceCard captured)
    {
        if (host.FaceId != "11013a" || host.Area.Type != DeckType.MainSchemesArea
            || card.Area.Host != host.ObjectId || card.Area.Type != DeckType.BeneathCardsPile
            || card.FaceId != "11023" || card.FaceUp || card.Owner != -1 || card.Incarnation != captured.Incarnation)
            throw new RulesNotImplementedException("Kang Dominion reveal requires its original concealed physical storage");
    }
}
