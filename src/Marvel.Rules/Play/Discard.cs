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
    /// <summary>Discards a card targeted by a card effect.</summary>
    public static void CardFromEffect(
        World world, ICardFacts facts, State.Card source, State.Card target,
        string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        if (!EffectCanRemove(world, facts, source, target))
        {
            throw new RulesNotImplementedException(
                $"card {source.ObjectId} cannot remove permanent card {target.ObjectId} "
                + "because they are not from the same set");
        }

        Card(world, target, trigger, events);
    }

    /// <summary>Whether a card effect may make one permanent target leave play.</summary>
    public static bool EffectCanRemove(
        World world, ICardFacts facts, State.Card source, State.Card target) =>
        // Removed is terminal even when an effect explicitly names it. Whether
        // another out-of-play area was expressly named belongs to the caller's
        // selector, not this Permanent rule primitive.
        target.Area.Type != DeckType.RemovedArea
        && (!DeckTypes.IsInPlay(target.Area.Type)
            || StateFields.Modified(world, target, "permanent", facts, world.Players) <= 0
            || SameSet(facts, source, target));

    /// <summary>Whether two printed cards belong to the same non-empty set.</summary>
    public static bool SameSet(ICardFacts facts, State.Card first, State.Card second)
    {
        string set = facts.EncounterSet(first.FaceId);
        return set.Length > 0
            && string.Equals(set, facts.EncounterSet(second.FaceId), StringComparison.Ordinal);
    }

    /// <summary>
    /// Puts a card in its owner's discard pile, or removes a spent status component.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="card">The card being discarded.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what moved.</param>
    /// <param name="verb">The semantic reason the card moved.</param>
    public static void Card(
        World world, State.Card card, string trigger, List<GameEvent> events,
        string verb = "Discard")
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        // rr:main-scheme-main-scheme-deck.6: "Main scheme cards cannot be
        // discarded from play." The attempted game function resolves without
        // moving the card or emitting a discard event.
        if (card.Area.Type == DeckType.MainSchemesArea)
        {
            return;
        }

        var constantsEnding = world.Effects.PreflightConstantsEnding(card);
        using var departure = constantsEnding.Begin();

        // `rr:attach-to.1`: "if the game element an attachment is attached to
        // leaves play, the attachment is discarded." Snapshot the areas because
        // discarding an attachment moves it and can itself detach hosted cards.
        if (DeckTypes.IsInPlay(card.Area.Type))
        {
            Attachments(world, card, trigger, events);
            ResetLeavingState(world, card, trigger, events);
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
        bool exposesIdentity = DeckTypes.IsConcealedPile(from.Type)
            || FacedownDrones.Is(card);
        int host = from.Host;
        World.MoveToTop(card, pile);
        if (exposesIdentity && card.FaceUp)
        {
            world.RecordInformation(InformationKind.Reveal);
        }

        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(pile),
            [new Landing(card.ObjectId, pile.Cards.Count - 1)])
        {
            Trigger = trigger, Verb = verb,
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
        if (!world.Effects.IsDeparting(host))
        {
            PreflightAttachments(world, host, direct);
        }

        // The engine commits a preflighted restored-Uses cascade as one state
        // snapshot. The Rules Reference does not order those simultaneous
        // zero-counter discards. Re-reading attachment legality between the
        // sequential event writes would let an intermediate board contradict
        // the snapshot that was proved before anything moved.

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

    /// <summary>Preflights a host after named direct cards leave by an earlier interrupt.</summary>
    internal static void PreflightAttachmentsExcept(
        World world, State.Card host, IReadOnlySet<int> leavingFirst)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(leavingFirst);
        PreflightAttachments(
            world, host,
            [.. AttachedTo(world, host.ObjectId)
                .Where(card => !leavingFirst.Contains(card.ObjectId))]);
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

        PreflightProjectedAttachments(world, host, descendants);
    }

    /// <summary>
    /// Validate a hosted tree whose cards are temporarily projected out of play.
    /// </summary>
    internal static void PreflightProjectedAttachments(
        World world, State.Card host, IEnumerable<State.Card> descendants)
    {
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

    /// <summary>Removes departure state that remains observable while out of play.</summary>
    public static void ResetLeavingState(
        World world, State.Card card, string trigger, List<GameEvent> events)
    {
        world.Effects.CardLeftPlay(card);

        // Most state fields are dormant outside play and reset if the card
        // becomes a new copy on re-entry. An acceleration token is different:
        // rr:acceleration-token.2.1 counts it from any in-play host, so clause
        // .3 expressly removes it when a non-main-scheme host leaves.
        if (card.Area.Type == DeckType.MainSchemesArea)
        {
            return;
        }

        long held = card.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken);
        if (held <= 0)
        {
            return;
        }

        card.PlaceTokens(EncounterDeck.AccelerationToken, -held);
        events.Add(new FieldSet(
            card.ObjectId, EncounterDeck.AccelerationToken, held, 0)
        {
            Trigger = trigger, Verb = "Remove",
        });
    }
}
