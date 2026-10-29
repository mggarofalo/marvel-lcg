using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Rules.Play.CardPayment;
using static Marvel.Rules.Play.CardPlayLegality;
using static Marvel.Rules.Play.CardControlTransfer;
using static Marvel.Rules.Play.CardEntry;
using static Marvel.Rules.Play.CardPlay;

namespace Marvel.Rules.Play;

/// <summary>Places played cards into their rules-defined destinations.</summary>
public static class CardEntry
{

    /// <summary>Where a played card goes — <c>rr:enters-play</c>.</summary>
    internal static void Enter(
        World world, ICardFacts facts, ICardPlayAbilities abilities, Seat seat, Card card,
        List<GameEvent> events, CardEntryChoices choices)
    {
        var targets = choices.Targets;
        var kind = EffectiveCards.Kind(card, facts);
        if (kind == CardKind.Ally && !choices.ApplyingAlly)
        {
            PlayerAllyEntry.SchedulePlay(world, seat, card, choices);
            return;
        }
        if (kind is CardKind.Event or CardKind.Resource)
        {
            // `rr:play-put-into-play.2`: "when an event card is played, place
            // it on the table, resolve its ability, and place the card in its
            // owner's discard pile." The ability *is* the card, so a card whose
            // ability nothing implements must say so rather than being a very
            // expensive discard.
            EventCardEntry.Resolve(world, abilities, seat, card, events);
            return;
        }

        var (into, upgradeHost) = PlayedCardDestination.Resolve(world, abilities, seat, card, kind, targets);

        var from = card.Area;
        World.MoveToTop(card, into);
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(into),
            [new Landing(card.ObjectId, into.Cards.Count - 1)])
        {
            Trigger = Verb,
            Verb = Verb,
        });

        choices.AfterMove?.Invoke();

        if (kind == CardKind.Upgrade)
        {
            events.Add(new CardAttached(card.ObjectId, upgradeHost)
            {
                Trigger = Verb,
                Verb = Verb,
            });
        }

        // `rr:enters-play`: the keywords that fire when a card enters play do
        // not care how it got there. Eighteen allies in the pool print
        // `rr:toughness`, and before this a played one got no tough status card
        // -- only a *revealed* card ran them.
        if (kind == CardKind.Ally && AllyEntryHealth.Settle(world, facts, card, events))
        {
            FinalizeAllyEntry(world, card, seat.Index);
        }
        else if (AllyLimit(world, facts, seat, card))
        {
            FinalizeAllyEntry(world, card, seat.Index);
        }
        else
        {
            Reveal.EnterPlay(world, facts, card, events, abilities: abilities);
        }
        if (!choices.ApplyingAlly)
            Played(world, seat, card, new PlayedCardPayment(card.ObjectId, card.Incarnation, seat.Index, choices.Paid), choices.EntryPayment);
        Restricted(world, facts, seat, card, events);
    }

    /// <summary>
    /// Ask which ally leaves when a player exceeds their ally limit —
    /// <c>rr:ally-limit</c>.
    /// </summary>
    internal static bool AllyLimit(World world, ICardFacts facts, Seat seat, Card played)
    {
        if (EffectiveCards.Kind(played, facts) != CardKind.Ally)
        {
            return false;
        }

        return CheckAllyLimit(world, facts, seat.Index, played.ObjectId);
    }

    /// <summary>Schedule the mandatory choice when a player exceeds their ally limit.</summary>
    internal static bool CheckAllyLimit(
        World world, ICardFacts facts, int player, int subject = -1)
    {
        var seat = world.Seats[player];
        // Discard is also used while building deliberately partial boards.
        // With no identity assigned there is no player ally-limit value yet.
        if (seat.IdentityCard is null)
        {
            return false;
        }

        int controlled = PlayerLimitProcedure.CountedAllies(world, player).Count;
        long limit = StateFields.Modified(
            world, seat.IdentityCard, "ally_limit", facts, world.Players);
        if (controlled <= limit)
        {
            return false;
        }

        // This is a mandatory rule choice rather than an occurrence, so it
        // opens no interrupt or response windows. It is scheduled before the
        // CardPlayed occurrence: the rule says the discard happens before
        // abilities that resolve upon entering play.
        if (!world.Agenda.Outstanding.Any(step =>
                step.What == Steps.ChooseAllyForLimit && step.Seat == player))
        {
            world.Agenda.Then(new PhaseStep(
                new PlayerLimitOperation(PlayerLimitKind.ChooseAllyForLimit),
                world.Agenda.Current?.Round ?? 0,
                0,
                Subject: subject,
                Seat: player,
                Plan: true
            ));
        }

        return true;
    }

    internal static void FinalizeAllyEntry(World world, Card ally, int player) =>
        world.Agenda.Then(new PhaseStep(
            new RevealStageOperation(RevealStageKind.FinalizeAllyEntry) { AllyIncarnation = ally.Incarnation },
            world.Agenda.Current?.Round ?? 0,
            0,
            Subject: ally.ObjectId,
            Seat: player,
            Plan: true
        ));

    /// <summary>Schedules the response window for an ally put into play.</summary>
    internal static void Entered(World world, Card ally, int player, EntryPayment? entryPayment = null)
    {
        // `rr:enters-play`: putting a card into play by a card ability is one
        // way it enters play. The transition therefore creates the same
        // after-entry response window as a card played from hand, but not the
        // `WhenCardPlayed` condition that rr:play-put-into-play.3 excludes.
        world.Agenda.Then(new PhaseStep(
            new LifecycleOperation(LifecycleKind.CardEntersPlay) { EntryPayment = entryPayment },
            world.Agenda.Current?.Round ?? 0,
            0,
            Subject: ally.ObjectId,
            Seat: player
        ));
    }

    internal static void Played(World world, Seat seat, Card card, PlayedCardPayment? payment = null,
        EntryPayment? entryPayment = null)
    {
        // The card has moved and all state it enters with has been applied.
        // The agenda supplies the ordinary response window, including the
        // optional responses `rr:ability.11` says must be chosen rather than
        // silently resolved or refused.
        world.Agenda.Then(new PhaseStep(
            new LifecycleOperation(LifecycleKind.CardPlayed) { Payment = payment, EntryPayment = entryPayment },
            world.Agenda.Current?.Round ?? 0,
            0,
            Index: seat.Index,
            Subject: card.ObjectId,
            Seat: seat.Index
        ));
    }

    /// <summary>
    /// A third restricted card forces one out — <c>rr:restricted</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "A player <b>can</b> play or put into play a restricted card even if they
    /// already control two restricted cards. However, if a player ever controls
    /// more than two restricted cards in play, they must <b>immediately</b>
    /// choose and discard from play restricted cards they control until they
    /// have only two."
    /// </para>
    /// <para>
    /// So the limit is not a play restriction — the card goes into play first
    /// and something has to leave afterwards. <c>rr:restricted.1</c> writes it
    /// as a <b>Forced Response</b> for exactly that reason.
    /// </para>
    /// <para>
    /// Which card leaves is the player's choice. The choice is an agenda
    /// continuation so it can suspend an enclosing card-play procedure and
    /// resume before that play's response window.
    /// </para>
    /// </remarks>
    internal static void Restricted(
        World world, ICardFacts facts, Seat seat, Card played, List<GameEvent> events)
    {
        if (StateFields.Modified(world, played, "restricted", facts, world.Players) <= 0)
        {
            return;
        }

        var held = new List<Card>();
        foreach (var area in world.Areas.ToList())
        {
            if (!DeckTypes.IsInPlay(area.Type))
            {
                continue;
            }

            held.AddRange(area.Cards.Where(card =>
                card.Owner == seat.Index
                && StateFields.Modified(world, card, "restricted", facts, world.Players) > 0));
        }

        if (held.Count > StateFieldCatalog.RestrictedLimit)
        {
            var choice = new PhaseStep(
                new PlayerLimitOperation(
                    PlayerLimitKind.ChooseRestrictedCard,
                    Candidates: [.. held.Select(card => card.ObjectId)]
                ),
                world.Agenda.Current?.Round ?? 0,
                0,
                Subject: played.ObjectId,
                Seat: seat.Index,
                Plan: true
            );

            if (world.Agenda.Occurrence is { } occurrence)
            {
                world.Agenda.ThenContinuation(choice, occurrence);
            }
            else
            {
                world.Agenda.Add(choice);
            }
        }
    }
}
