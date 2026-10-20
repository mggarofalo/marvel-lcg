using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Stores cards out of play beneath a host, preserving physical ownership.</summary>
public static class BeneathCards
{
    /// <summary>The hosted out-of-play zones used for faceup and concealed stored cards.</summary>
    public static bool IsStored(DeckType type) => type is DeckType.BeneathCardsArea or DeckType.BeneathCardsPile;

    /// <summary>Stores the printed selection; this is neither playing nor discarding that card.</summary>
    public static void Place(World world, Card card, Card host, bool faceUp, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(events);
        if (!world.Cards.Contains(card) || !world.Cards.Contains(host) || card == host
            || !DeckTypes.IsInPlay(host.Area.Type))
            throw new ArgumentException("stored cards require a different in-play host in this world");
        // ruling:6768213a7e0f4606: cards attached to Quiver are not considered in play.
        // rr:in-play-and-out-of-play.10: "Facedown cards attached to in-play cards are out of play."
        if (!SideSchemeDeparture.Allowed(world, card)) return;
        var from = card.Area;
        bool revealsFace = faceUp && !card.FaceUp;
        var destination = world.AreaOf(faceUp ? DeckType.BeneathCardsArea : DeckType.BeneathCardsPile,
            host.Area.PlayArea, host: host.ObjectId, cardOwner: card.Owner);
        var ending = world.Effects.PreflightConstantsEnding(card);
        using var departure = ending.Begin();
        if (DeckTypes.IsInPlay(from.Type))
        {
            Discard.Attachments(world, card, trigger, events);
            Discard.ResetLeavingState(world, card, trigger, events);
        }
        World.MoveToTop(card, destination);
        if (revealsFace) world.RecordInformation(InformationKind.Reveal);
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(destination),
            [new Landing(card.ObjectId, destination.Cards.Count - 1)])
        { Trigger = trigger, Verb = "Store_Beneath" });
        events.Add(new CardAttached(card.ObjectId, host.ObjectId) { Trigger = trigger, Verb = "Store_Beneath" });
        ending.Complete(trigger, events);
    }

    /// <summary>Returns the host's stored cards in stable physical area and card order.</summary>
    public static IReadOnlyList<Card> Of(World world, Card host) =>
        [.. world.Areas.Where(area => area.Host == host.ObjectId && IsStored(area.Type)).SelectMany(area => area.Cards)];
}
