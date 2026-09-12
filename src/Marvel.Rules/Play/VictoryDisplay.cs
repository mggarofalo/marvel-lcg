using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Moves defeated cards and their attachments to the victory display.</summary>
public static class VictoryDisplay
{

    /// <summary>
    /// A card whose departure is replaced by Victory goes to the victory
    /// display — <c>rr:victory-x</c>.
    /// </summary>
    /// <remarks>
    /// <c>rr:victory-x.2</c>: "a character or side scheme with the victory X
    /// keyword is placed in the victory display <b>when it is defeated</b>",
    /// which <c>.1.1</c> writes as "<b>When Defeated</b>: add this card to the
    /// victory display". Instead of the discard pile, not as well as it.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="card">The card moving to the victory display.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what moved.</param>
    /// <returns>Whether it went there.</returns>
    public static bool ToVictoryDisplay(
        World world, ICardFacts facts, Card card, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);

        // Presence and value are separate: Victory 0 still supplies this
        // replacement ability even though it contributes no points.
        if (!Timing.Keywords.Has(world, card, "victory", facts))
        {
            return false;
        }

        MoveToVictoryDisplay(world, card, trigger, events);
        return true;
    }

    /// <summary>Commits a Victory destination already proved on the trigger board.</summary>
    internal static void MoveToVictoryDisplay(
        World world, Card card, string trigger, List<GameEvent> events,
        string verb = "Victory", string? subject = null)
    {
        var display = world.AreaOf(DeckType.VictoryDisplay);
        var from = card.Area;
        var constantsEnding = world.Effects.PreflightConstantsEnding(card);
        using var departure = constantsEnding.Begin();
        Discard.Attachments(world, card, trigger, events);
        Discard.ResetLeavingState(world, card, trigger, events);
        World.MoveToTop(card, display);
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(display),
            [new Landing(card.ObjectId, display.Cards.Count - 1)])
        {
            Trigger = trigger,
            Verb = verb,
            Subjects = subject is null
                ? null
                : new Dictionary<int, string> { [card.ObjectId] = subject },
        });
        constantsEnding.Complete(trigger, events);
    }

    /// <summary>Total points currently in the shared victory display.</summary>
    public static long VictoryPoints(World world, ICardFacts facts) =>
        world.AreaOf(DeckType.VictoryDisplay).Cards.Sum(card =>
            StateFields.Modified(world, card, "victory", facts, world.Players));

    /// <summary>Moves victory attachments away before ordinary hosted-card cleanup.</summary>
    internal static void VictoryAttachments(
        World world, IReadOnlyList<Card> attachments, string trigger,
        List<GameEvent> events)
    {
        foreach (var attachment in attachments)
        {
            MoveToVictoryDisplay(world, attachment, trigger, events);
        }
    }

    internal static List<Card> VictoryAttachmentsOn(
        World world, ICardFacts facts, Card host) =>
    [
        .. world.Areas
            .Where(area => area.Host == host.ObjectId
                && DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Where(card => facts.Kind(card.FaceId) is
                CardKind.Attachment or CardKind.Upgrade)
            .Where(card => Timing.Keywords.Has(world, card, "victory", facts)),
    ];

    /// <summary>Preflights the hosted tree in its Victory-interrupt order.</summary>
    internal static List<Card> PreflightDefeatAttachments(
        World world, ICardFacts facts, Card host)
    {
        var victory = VictoryAttachmentsOn(world, facts, host);

        // A Victory attachment leaves before its host. Permanent on that same
        // card therefore never reaches rr:permanent.5, but any hosted
        // descendant still has to be proved removable from the Victory card.
        foreach (var attachment in victory)
        {
            Discard.PreflightAttachments(world, attachment);
        }

        Discard.PreflightAttachmentsExcept(
            world, host, victory.Select(card => card.ObjectId).ToHashSet());
        return victory;
    }

    /// <summary>Moves Victory interrupts and their defeated host as one transaction.</summary>
    internal static void MoveDefeatedCard(
        World world, ICardFacts facts, Card host, string trigger,
        List<GameEvent> events)
    {
        string? subject = FacedownDrones.Is(host)
            ? FacedownDrones.EffectiveTitle
            : null;
        var victory = PreflightDefeatAttachments(world, facts, host);
        bool hostHasVictory = Timing.Keywords.Has(world, host, "victory", facts);
        var victoryRoots = victory.Select(card => card.ObjectId).ToHashSet();
        var constantsEnding = world.Effects.PreflightConstantsEnding(
            DefeatDepartureCards(world, [host]), victoryRoots);
        using var departure = constantsEnding.Begin();

        VictoryAttachments(world, victory, trigger, events);
        if (hostHasVictory)
        {
            MoveToVictoryDisplay(
                world, host, trigger, events, verb: "Defeat", subject: subject);
        }
        else
        {
            Discard.Card(world, host, trigger, events, verb: "Defeat", subject: subject);
        }

        constantsEnding.Complete(trigger, events);
    }

    /// <summary>The complete physical card set removed by one host defeat.</summary>
    internal static List<Card> DefeatDepartureCards(
        World world, IReadOnlyList<Card> roots)
    {
        var cards = new List<Card>();
        var seen = new HashSet<int>();
        var pending = new Stack<Card>(roots.Reverse());
        while (pending.TryPop(out var card))
        {
            if (!seen.Add(card.ObjectId))
            {
                throw new RulesNotImplementedException(
                    $"attachment {card.ObjectId} forms a hosting cycle");
            }
            cards.Add(card);
            foreach (var child in world.Areas
                         .Where(area => area.Host == card.ObjectId)
                         .SelectMany(area => area.Cards)
                         .Reverse())
            {
                pending.Push(child);
            }
        }
        return cards;
    }

    /// <summary>
    /// A villain stage is defeated — <c>rr:villain-defeat</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Remove the current stage of the villain deck from the game. The next
    /// sequential stage of the villain deck is revealed. Set the villain's hit
    /// point dial as indicated by that stage. <b>If the final stage of the
    /// villain deck is defeated, the players win the game.</b>"
    /// </para>
    /// <para>
    /// <c>rr:villain-defeat.2</c>: "excess damage that is dealt to defeat a
    /// villain stage does not carry over to the new stage" — which is why the
    /// new stage starts with no damage rather than inheriting any.
    /// </para>
    /// </remarks>
    internal static void VillainStage(
        World world, ICardFacts facts, Card villain, string trigger, List<GameEvent> events)
    {
        var deck = world.AreaOf(DeckType.VillainDeck);
        var following = deck.Cards.Count > 0 ? deck.Cards[^1] : null;
        bool carriesToFollowing = following is not null && string.Equals(
            facts.Title(villain.FaceId), facts.Title(following.FaceId),
            StringComparison.Ordinal);
        var constantsEnding = world.Effects.PreflightConstantsEnding(
            villain, includeHostedCards: !carriesToFollowing);
        using var departure = constantsEnding.Begin();
        if (!carriesToFollowing)
        {
            // Same-title stages inherit hosted cards. Every other departure,
            // including the final stage, discards them before the host moves.
            Discard.Attachments(world, villain, trigger, events);
        }

        var removed = world.AreaOf(DeckType.RemovedArea);
        var from = villain.Area;
        World.MoveToTop(villain, removed);
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(removed),
            [new Landing(villain.ObjectId, removed.Cards.Count - 1)])
        {
            Trigger = trigger,
            Verb = "Defeat",
        });
        constantsEnding.Complete(trigger, events);

        var next = deck.TakeTop();
        if (next is null)
        {
            world.Finish(Outcome.PlayersWin);
            return;
        }

        var area = world.AreaOf(DeckType.VillainArea);
        World.MoveToTop(next, area);
        next.TurnFaceUp();
        events.Add(new CardsMoved(
            Places.Reference(deck), Places.Reference(area),
            [new Landing(next.ObjectId, area.Cards.Count - 1)])
        {
            Trigger = trigger,
            Verb = "Reveal",
        });

        // `rr:villain-defeat.3.2` before either of the two below, so that a
        // tough status card carried over from the old stage is already on the
        // new one when toughness looks for it.
        Defeat.Inherit(world, facts, villain, next, trigger, events);

        // The stage came out of the villain deck and into the villain's play
        // area, and `rr:enters-play` is "any time when a card transitions from
        // an out-of-play area into play" -- so the keywords that fire on
        // entering play fire here. `rr:villain-defeat.3.1` makes the new stage
        // "the same character" for card abilities, which is a claim about who
        // the character is rather than about the card having been in play: the
        // card itself is a different card, and it was in the deck a moment ago.
        Reveal.EnterPlay(world, facts, next, events);

        // `rr:when-revealed-abilities`: "when a player reveals a card from the
        // encounter deck, a new scheme stage, **or a new villain stage**, all
        // 'When Revealed' abilities on the card resolve." Last, because
        // `rr:reveal.step.3` puts the card's own text after the placement and
        // the keywords -- and `.3` there, with `rr:villain-defeat.1`, is why
        // nothing between here and the deck gets to cancel it.
        events.AddRange(world.EncounterAbilities.WhenRevealed(world, next, world.FirstPlayer));
    }
}
