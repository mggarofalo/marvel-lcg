using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>
/// Discarding a card — <c>rr:discard</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:discard-pile</c>: a discarded card goes to <b>its owner's</b> discard
/// pile, and an encounter card's owner is the scenario. That is why this reads
/// the card's owner rather than being told where to put it: a caller that had to
/// choose could choose wrong, and a player card in the encounter discard is a
/// board nothing else would notice.
/// </para>
/// <para>
/// A rules primitive rather than something a card does for itself, for the same
/// reason <see cref="Draw"/> is one: it is a rule of the game that happens to be
/// reachable from card text.
/// </para>
/// </remarks>
public static class Discard
{
    /// <summary>
    /// Puts a card in its owner's discard pile, or removes a spent status component.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="card">The card being discarded.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what moved.</param>
    public static void Card(World world, State.Card card, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        var constantsEnding = world.Effects.PreflightConstantsEnding(card);
        using var departure = constantsEnding.Begin();

        // `rr:attach-to.1`: "if the game element an attachment is attached to
        // leaves play, the attachment is discarded." Snapshot the areas because
        // discarding an attachment moves it and can itself detach hosted cards.
        if (DeckTypes.IsInPlay(card.Area.Type))
        {
            Attachments(world, card, trigger, events);
        }

        // A status card is discarded by its keyword rule, but it is not an
        // encounter card and therefore has no encounter discard pile. The
        // engine chooses RemovedArea as the out-of-play home for spent status
        // components; the Rules Reference does not name a separate status pile.
        var pile = card.Area.Type == DeckType.StatusArea
            ? world.AreaOf(DeckType.RemovedArea)
            : card.Owner < 0
                ? world.AreaOf(DeckType.EncounterDiscardPile)
                : world.AreaOf(
                    DeckType.DiscardPile, PlayArea.Of(card.Owner), cardOwner: card.Owner);

        var from = card.Area;
        int host = from.Host;
        World.MoveToTop(card, pile);

        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(pile),
            [new Landing(card.ObjectId, pile.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = "Discard",
        });

        constantsEnding.Complete(trigger, events);

        // `rr:player-deck.4`: a deck that emptied beside an empty discard pile
        // "does not reset until there is at least one card in the player's
        // discard pile, **then** the player deals themself one facedown
        // encounter card". This is *then* -- a card has just landed there.
        if (card.Owner >= 0)
        {
            PlayerDeck.Reset(world, card.Owner, events);
        }

        if (host >= 0)
        {
            // A card that leaves play stops being attached, and a client that
            // drew it hanging off the villain has to be told to take it away.
            events.Add(new CardDetached(card.ObjectId, host)
            {
                Trigger = trigger, Verb = "Discard",
            });
        }

        // A support such as The Triskelion can leave play and reduce a
        // player's modified ally limit. `rr:ally-limit` applies whenever the
        // count is over the live limit, not only when the latest ally entered.
        foreach (int player in world.PlayerOrder)
        {
            CardPlay.CheckAllyLimit(world, world.Facts, player);
        }
    }

    /// <summary>Discard every non-permanent card hosted by a game element leaving play.</summary>
    public static void Attachments(
        World world, State.Card host, string trigger, List<GameEvent> events)
    {
        var direct = AttachedTo(world, host.ObjectId);
        PreflightAttachments(world, host, direct);

        foreach (var attached in direct)
        {
            Card(world, attached, trigger, events);
        }
    }

    /// <summary>Prove that every hosted card can leave before moving any of them.</summary>
    public static void PreflightAttachments(World world, State.Card host)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(host);
        PreflightAttachments(world, host, AttachedTo(world, host.ObjectId));
    }

    private static void PreflightAttachments(
        World world, State.Card host, IReadOnlyList<State.Card> direct)
    {
        var descendants = new List<State.Card>();
        var pending = new Stack<State.Card>(direct.AsEnumerable().Reverse());
        var seen = new HashSet<int> { host.ObjectId };
        while (pending.TryPop(out var attached))
        {
            if (!seen.Add(attached.ObjectId))
            {
                throw new RulesNotImplementedException(
                    $"attachment {attached.ObjectId} forms a hosting cycle");
            }

            descendants.Add(attached);
            foreach (var child in AttachedTo(world, attached.ObjectId).AsEnumerable().Reverse())
            {
                pending.Push(child);
            }
        }

        if (descendants.FirstOrDefault(attached => StateFields.Modified(
                world, attached, "permanent", world.Facts, world.Players) > 0)
            is { } permanent)
        {
            // `rr:permanent.5` resolves the attachment's attach-to text again
            // and removes it only if no valid target exists. Preflight the
            // complete tree so no ordinary sibling moves before this refusal.
            throw new RulesNotImplementedException(
                $"permanent attachment {permanent.ObjectId} lost host "
                + $"{host.ObjectId}, and rr:permanent.5 is not implemented");
        }
    }

    private static List<State.Card> AttachedTo(World world, int host) =>
    [
        .. world.Areas
            .Where(area => area.Host == host)
            .SelectMany(area => area.Cards),
    ];
}
