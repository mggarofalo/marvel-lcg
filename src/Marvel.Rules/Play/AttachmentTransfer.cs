using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Moves an attachment already in play onto another host without it leaving play.</summary>
/// <remarks>
/// <c>rr:attach-to.3.1</c>: an ability that attaches a card to a specific game element replaces
/// its printed "attach to" instruction. The attachment stays in play, so it keeps its
/// incarnation, tokens and readiness (<c>rr:attach-to.2</c>). The attachment area mirrors
/// <see cref="CardEntryDestination"/>.
/// </remarks>
public static class AttachmentTransfer
{
    /// <summary>Attaches <paramref name="card"/>, in play, to <paramref name="host"/>.</summary>
    public static void Move(World world, Card card, Card host, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(events);
        // Equivalent mutant: dropping this guard. Callers move in-play attachments onto in-play hosts.
        if (!DeckTypes.IsInPlay(card.Area.Type) || !DeckTypes.IsInPlay(host.Area.Type) || card == host)
            throw new RulesNotImplementedException(
                $"attachment {card.ObjectId} cannot move onto host {host.ObjectId} outside play");
        int was = card.Area.Host;
        var from = card.Area;
        var onto = world.AreaOf(DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId, host.Area.CardOwner);
        World.MoveToTop(card, onto);
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(onto),
            [new Landing(card.ObjectId, onto.Cards.Count - 1)]) { Trigger = trigger, Verb = "Attach" });
        if (was >= 0) events.Add(new CardDetached(card.ObjectId, was) { Trigger = trigger, Verb = "Attach" });
        events.Add(new CardAttached(card.ObjectId, host.ObjectId) { Trigger = trigger, Verb = "Attach" });
    }
}
