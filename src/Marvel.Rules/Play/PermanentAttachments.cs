using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>A permanent attachment whose host leaves play — <c>rr:permanent.5</c>.</summary>
/// <remarks>
/// "If the card to which a permanent attachment was attached leaves play, resolve that
/// attachment's 'attach to' text. If it has none or that text does not have a valid target,
/// remove the attachment from the game." Ruling 8944f7176577c908 applies this to Power Stone
/// (16149) on a defeated identity: it attaches to the villain again. A direct attachment with
/// nothing attached to it is modelled when the ability book records its "attach to" text and
/// that text has one valid target or none. One hosted on another attachment, one with cards
/// attached, one whose text the book does not record, and one with several valid targets,
/// which would need the first player's choice (<c>rr:first-player.1</c>), raise.
/// </remarks>
internal static class PermanentAttachments
{
    /// <summary>Whether rr:permanent.5 is modelled for <paramref name="attached"/>, hosted directly on <paramref name="host"/>.</summary>
    // Equivalent mutants: accepting several targets (Power Stone's "Attach to the villain"
    // names one card) and dropping the attachment-kind test (callers pass hosted cards, and
    // no admitted permanent upgrade or support is hosted on a card that leaves play).
    internal static bool Modelled(World world, Card host, Card attached) =>
        attached.Area.Host == host.ObjectId
        && StateFields.Modified(world, attached, "permanent", world.Facts, world.Players) > 0
        && EffectiveCards.Kind(attached, world.Facts) == CardKind.Attachment
        && Targets(world, host, attached) is { Count: <= 1 }
        && !world.Areas.Any(area => area.Host == attached.ObjectId && area.Cards.Count > 0);

    /// <summary>Resolves the attachment's "attach to" text away from its departing host.</summary>
    internal static void Resolve(World world, Card host, Card attached, string trigger, List<GameEvent> events)
    {
        if (!Modelled(world, host, attached))
            throw new RulesNotImplementedException(
                $"permanent attachment {attached.ObjectId} lost host {host.ObjectId}, and its "
                + "rr:permanent.5 'attach to' resolution is not modelled");
        if (Targets(world, host, attached) is [int target])
            AttachmentTransfer.Move(world, attached, world.Cards[target], trigger, events);
        else
            RemoveFromGame(world, attached, trigger, events);
    }

    // Equivalent mutants: dropping the host or departing test. The only modelled text names
    // the villain, which is neither an eliminated identity nor a defeated minion. A villain's
    // final stage leaving play ends the game first; an earlier stage passes its attachments to
    // the next (rr:villain-defeat.3.2).
    // Null when the ability book records no "attach to" text for the attachment.
    private static IReadOnlyList<int>? Targets(World world, Card host, Card attached) =>
        world.PlacementAbilities.AttachmentTargets(world, attached, world.FirstPlayer) is { } targets
            ? [.. targets.Where(target => target != host.ObjectId
                && DeckTypes.IsInPlay(world.Cards[target].Area.Type)
                && !world.Effects.IsDeparting(world.Cards[target]))]
            : null;

    private static void RemoveFromGame(World world, Card attached, string trigger, List<GameEvent> events)
    {
        var constantsEnding = world.Effects.PreflightConstantsEnding(attached);
        using var departure = constantsEnding.Begin();
        Discard.ResetLeavingState(world, attached, trigger, events);
        var from = attached.Area;
        var removed = world.AreaOf(DeckType.RemovedArea);
        World.MoveToTop(attached, removed);
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(removed),
            [new Landing(attached.ObjectId, removed.Cards.Count - 1)]) { Trigger = trigger, Verb = "Remove" });
        constantsEnding.Complete(trigger, events);
    }
}
