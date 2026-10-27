using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal static class AbilityEncounterReveal
{
    /// <summary>Moves a card now so the executor can schedule its reveal procedure.</summary>
    /// <remarks>
    /// Revealing is not dealing: <c>rr:deal</c> leaves a card facedown for the
    /// villain phase, while <c>rr:reveal</c> opens its own interrupt and response
    /// windows. The move precedes the scheduled procedure so later text sees
    /// the card in the revealing area rather than its former deck or pile.
    /// </remarks>
    internal static AbilityRevealRequest? Prepare(
        Card? card, AbilityDeckAndRevealContext context)
    {
        if (card is null) return null;
        var from = card.Area;
        var revealing = context.World.AreaOf(DeckType.RevealingArea);
        EncounterRevealExposure.Expose(context.World, card, context.Trigger, context.Events);
        World.MoveToTop(card, revealing);
        context.Events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(revealing),
            [new Landing(card.ObjectId, revealing.Cards.Count - 1)])
        {
            Trigger = context.Trigger,
            Verb = "Reveal",
        });
        int player = context.Player >= 0 ? context.Player : context.World.FirstPlayer;
        return new AbilityRevealRequest(card.ObjectId, player,
            new Marvel.Rules.Timing.EncounterRevealOrigin(card.ObjectId, Places.Reference(from)));
    }

}
