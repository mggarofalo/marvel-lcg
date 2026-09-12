using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>A read-only projection of a forced would-be-defeated interrupt.</summary>

internal static class EncounterRevealProcedure
{
    /// <summary>Step 3. One card each, plus one per hazard icon in play.</summary>
    /// <remarks>
    /// <c>rr:villain-phase.step.3</c>: "Deal one encounter card to each player.
    /// Deal one additional card for each hazard icon on a card in play. These
    /// additional cards are dealt in player order."
    /// <para>
    /// Nothing here schedules a reveal. Step 4 drains the queue instead, which
    /// is what lets a card dealt at any other moment — by an ability, or by a
    /// player's deck running out mid-turn — be revealed in the same step as the
    /// rest.
    /// </para>
    /// </remarks>
    internal static void DealEncounterCards(
        World world, ICardFacts facts, List<GameEvent> events)
    {
        foreach (int seat in world.PlayerOrder)
        {
            if (Deal.EncounterCard(world, seat, "villain phase", events) is null)
            {
                return;
            }
        }

        // `rr:hazard-icon`: "for each hazard icon on cards in play, deal one
        // player one additional card *(not one card per player)*. Additional
        // cards are dealt in player order" -- so these go round the table one
        // at a time, wrapping, rather than one round per icon.
        long icons = Deal.HazardIcons(world, facts);
        for (long dealt = 0; dealt < icons; dealt++)
        {
            int seat = (world.FirstPlayer + (int)(dealt % world.Players)) % world.Players;
            if (Deal.EncounterCard(world, seat, "hazard", events) is null)
            {
                return;
            }
        }
    }

    /// <summary>Step 4, one card at a time, until the queue is empty.</summary>
    internal static void RevealNextEncounterCard(World world, PhaseStep step)
    {
        if (Deal.NextToReveal(world) is not { } next)
        {
            return;
        }

        // The reveal is an occurrence with its own windows; this heading is
        // not. Scheduling itself *after* the reveal is what makes step 4 a
        // loop -- a card revealed here can deal another, and `rr:deal.1` puts
        // that one in the same step.
        //
        // **The order of these two calls is the loop's termination.**
        // `Agenda.Then` appends in call order, so the reveal has to be
        // scheduled first; the other way round, this heading runs again with
        // the card still in the queue and schedules itself forever.
        world.Agenda.Then(new PhaseStep(
            Steps.RevealEncounterCard, step.Round, 4,
            Index: step.Index, Subject: next.Card.ObjectId, Seat: next.Player));
        world.Agenda.Then(new PhaseStep(
            Steps.RevealEncounterCards, step.Round, 4, Index: step.Index + 1, Plan: true));
    }

    /// <summary>Step 4. Each player reveals their cards, in the order dealt.</summary>
    internal static void RevealEncounterCard(
        World world, ICardFacts facts, IRevealCardAbilities abilities, Card card, int player,
        int round, List<GameEvent> events)
    {
        var revealOccurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("a revealing card has no occurrence");

        // An interrupt to this reveal may discard the card and replace its
        // effects before the occurrence applies. A card no longer in either
        // reveal staging area cannot enter play or resolve printed text from
        // the stale scheduled step. Cards explicitly revealed by an ability
        // begin in RevealingArea; villain-phase cards begin in the dealt queue.
        if (card.Area.Type is not (DeckType.DealtEncounterCardsDeck
            or DeckType.RevealingArea))
        {
            return;
        }

        // `rr:reveal.4.1` -- "if the card specifies a player to give it to,
        // **that player is considered to be revealing it**." One reassignment
        // and not a special case at the placement, because being the revealing
        // player is the whole of what the rule says: `rr:obligation.1` makes
        // every "you" on the card point at the player whose area it is in, and
        // `rr:obligation.4` puts it in the named player's.
        //
        // At one player the named player and the revealing player are the same
        // seat, which is why this went unnoticed. Above one they are not.
        switch (RevealKeywords.Names(world, facts, card))
        {
            case null:
                break;

            case >= 0 and var named:
                player = named;
                break;

            default:
                // `rr:obligation.5` -- "if an obligation cannot be given to the
                // specified player for any reason, **ignore the card's
                // ability, remove it from the game, and reveal an additional
                // encounter card**." Dealt rather than revealed directly: step
                // 4 is a loop over what a player has been dealt, so a card put
                // in that queue is revealed by the same step -- which is how
                // `rr:surge` already works.
                var gone = world.AreaOf(DeckType.RemovedArea);
                var was = card.Area;
                World.MoveToTop(card, gone);
                events.Add(new CardsMoved(
                    Places.Reference(was), Places.Reference(gone),
                    [new Landing(card.ObjectId, gone.Cards.Count - 1)])
                {
                    Trigger = "villain phase",
                    Verb = "Remove",
                });

                Deal.EncounterCard(world, player, "obligation", events);
                return;
        }

        // Same reason as the boost card: the revealing area is where an
        // encounter card registers its pools.
        World.MoveToTop(
            card,
            world.AreaOf(DeckType.RevealingArea, PlayArea.Of(player)));
        card.TurnFaceUp();
        world.RecordInformation(InformationKind.Reveal);
        events.Add(new CardsFlipped([card.ObjectId], true)
        {
            Trigger = "villain phase",
            Verb = "Reveal",
        });

        bool uniqueBlocked = facts.Kind(card.FaceId) != CardKind.EncounterVillain
            && Uniqueness.IsBlocked(world, facts, card);
        if (uniqueBlocked)
        {
            Reveal.Resolve(world, facts, card, player, events, revealOccurrence);
            // Its own reveal/entry effects are ignored. The reveal occurrence
            // still reaches its response boundary before the queue continues.
            world.Agenda.BeforeResponses(revealOccurrence);
            return;
        }

        if (facts.Kind(card.FaceId) == CardKind.Attachment
            && abilities.AttachmentTargets(world, card) is { Count: > 1 } targets)
        {
            world.Agenda.ThenContinuation(new PhaseStep(
                Steps.ChooseAttachmentTarget,
                round,
                2,
                Subject: card.ObjectId,
                Seat: player,
                Plan: true,
                ProcedureCandidates: [.. targets],
                AbilityOccurrence: revealOccurrence), revealOccurrence);
            world.Agenda.BeforeResponses(revealOccurrence);
            return;
        }

        // `rr:reveal.step.2` -- **where the card goes is decided by its type**,
        // and it happens before step 3's "When Revealed" abilities. A minion
        // that entered play is already engaged when its own ability resolves.
        Reveal.Resolve(world, facts, card, player, events, world.Agenda.Occurrence);

        FinishEncounterReveal(
            world, facts, abilities, card, player, round, revealOccurrence, events);
    }

    internal static void FinishEncounterReveal(
        World world, ICardFacts facts, IRevealCardAbilities abilities, Card card, int player,
        int round, Occurrence revealOccurrence, List<GameEvent> events)
    {

        // Step 3. "Resolve each **When Revealed** ability on that card
        // *(including those provided by keywords)*."
        //
        // `rr:forced.5`: "if two or more forced abilities would initiate at
        // the same moment, the first player determines the order in which the
        // abilities initiate." Keyword-provided and printed When Revealed
        // abilities are therefore one ordering question.
        var occurrence = revealOccurrence;
        if (!abilities.CancelWhenRevealed(world, card, player, occurrence))
        {
            var keyword = RevealKeywords.KeywordAbilities(world, facts, card, player);
            var printed = abilities.WhenRevealedAbilities(world, card, player);
            var simultaneous = keyword.Concat(printed).ToList();
            if (simultaneous.Count > 1)
            {
                if (facts.Kind(card.FaceId) == CardKind.Treachery)
                {
                    occurrence.BeginCard(card.ObjectId, simultaneous);
                }
                var handles = simultaneous
                    .Select((_, index) => 1_000_000 + index)
                    .ToList();
                world.Agenda.ThenContinuation(new PhaseStep(
                    Steps.ChooseRevealAbility,
                    round,
                    3,
                    Subject: card.ObjectId,
                    Seat: player,
                    Plan: true,
                    ProcedureAbilities: simultaneous,
                    ProcedureCandidates: handles,
                    ProcedureOccurrence: occurrence), occurrence);
                world.Agenda.BeforeResponses(occurrence);
                return;
            }

            RevealKeywords.Keywords(world, facts, abilities, card, player, events, occurrence);
            events.AddRange(abilities.WhenRevealed(world, card, player, occurrence));
        }

        FinishEncounterRevealTail(
            world, facts, card, player, round, revealOccurrence, events);
    }

    internal static void FinishEncounterRevealTail(
        World world, ICardFacts facts, Card card, int player, int round,
        Occurrence revealOccurrence, List<GameEvent> events,
        bool beforeResponses = true)
    {
        // `rr:quickstrike.2` puts this after the card's own abilities, and it
        // is the one keyword that does something *after* them rather than
        // beside them.
        bool quickstrike = Reveal.QuickstrikeApplies(world, facts, card, player);
        bool teamwork = Reveal.TeamworkApplies(world, facts, card);
        if (quickstrike && teamwork)
        {
            world.Agenda.ThenContinuation(new PhaseStep(
                Steps.ChoosePostRevealAbility,
                round,
                3,
                Subject: card.ObjectId,
                Seat: player,
                Plan: true,
                ProcedureOccurrence: revealOccurrence), revealOccurrence);
            if (beforeResponses)
            {
                world.Agenda.BeforeResponses(revealOccurrence);
            }
            return;
        }

        Reveal.Quickstrike(world, facts, card, player, round);
        Reveal.Teamwork(world, facts, card, player, round);

        FinishEncounterRevealDiscard(
            world, facts, card, player, round, revealOccurrence, beforeResponses);
    }

    internal static void FinishEncounterRevealDiscard(
        World world, ICardFacts facts, Card card, int player, int round,
        Occurrence revealOccurrence, bool beforeResponses = true)
    {

        // Step 4. "If the card is a treachery, discard it." This is agenda
        // work rather than an inline move because `rr:treachery.2.1` keeps a
        // treachery whose last effect initiates activations faceup until all of
        // them finish. `Then` places this behind any activations or choices the
        // When Revealed text just scheduled.
        if (facts.Kind(card.FaceId) == CardKind.Treachery)
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

    internal static void DiscardRevealedTreachery(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        var card = world.Cards[step.Subject];
        // The area check keeps an ability that moved the treachery from being
        // undone. The kind check makes a reconstructed agenda refuse stale or
        // malformed continuation data rather than discarding another type.
        if (facts.Kind(card.FaceId) != CardKind.Treachery
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
