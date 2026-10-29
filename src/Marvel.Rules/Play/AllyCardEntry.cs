using Marvel.Rules.Events;
using Marvel.Rules.State;
using static Marvel.Rules.Play.CardEntry;
using static Marvel.Rules.Play.CardPlayLegality;

namespace Marvel.Rules.Play;

/// <summary>Owns ally entry under an engine-granted put-into-play permission.</summary>
public static class AllyCardEntry
{
    /// <summary>Whether an ally can enter under the named controller without being played.</summary>
    public static bool CanPut(World world, Card ally, int controller) =>
        controller >= 0 && controller < world.Players
        && EffectiveCards.Kind(ally, world.Facts) == CardKind.Ally
        && !DeckTypes.IsInPlay(ally.Area.Type)
        && ally.Area.Type != DeckType.RemovedArea
        && (world.Facts.RequiredForm(ally.FaceId) is not { } form
            || Forms.In(world, world.Seats[controller], world.Facts, form))
        && !Uniqueness.IsBlocked(world, world.Facts, ally, PlayArea.Of(controller));

    /// <summary>Put into play the exact ally whose entry cost was paid.</summary>
    public static void PutPaid(
        World world, ICardFacts facts, ICardPlayAbilities abilities, EntryPayment payment,
        string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(payment);
        if (payment.Card < 0 || payment.Card >= world.Cards.Count)
            throw new RulesNotImplementedException("the paid ally does not exist in this game");
        var ally = world.Cards[payment.Card];
        Put(world, facts, abilities, new AllyEntryRequest(ally, payment.Player, trigger, payment), events);
    }

    internal static void Put(
        World world, ICardFacts facts, ICardPlayAbilities abilities, AllyEntryRequest request,
        List<GameEvent> events)
    {
        var (ally, controller, trigger, payment) = request;
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(ally);
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentOutOfRangeException.ThrowIfNegative(controller);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(controller, world.Players);

        if (!ValidateEntry(world, facts, request)) return;

        var from = ally.Area;
        int previousController = from.PlayArea.IsPlayers
            ? from.PlayArea.Player
            : ally.Owner;
        var into = world.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(controller), cardOwner: ally.Owner);

        World.MoveToTop(ally, into);
        events.Add(new CardsMoved(
            Places.Reference(from), Places.Reference(into),
            [new Landing(ally.ObjectId, into.Cards.Count - 1)])
        {
            Trigger = trigger,
            Verb = "Put_Into_Play",
        });

        if (previousController != controller)
        {
            events.Add(new ControlChanged(ally.ObjectId, previousController, controller)
            {
                Trigger = trigger,
                Verb = "Put_Into_Play",
            });
        }

        if (AllyLimit(world, facts, world.Seats[controller], ally))
        {
            FinalizeAllyEntry(world, ally, controller);
        }
        else
        {
            Reveal.EnterPlay(world, facts, ally, events, abilities: abilities);
        }
        // rr:triggering-condition.1: each response triggers once per occurrence.
        // This path owns the entry window; ordinary play owns its combined
        // played/entry window. Both wait for the mandatory ally-limit choice.
        Entered(world, ally, controller, payment);
    }
    private static bool ValidateEntry(World world, ICardFacts facts, AllyEntryRequest request)
    {
        var (ally, controller, _, payment) = request;
        if (EffectiveCards.Kind(ally, facts) != CardKind.Ally)
        {
            throw new RulesNotImplementedException(
                $"card {ally.ObjectId} is not an ally and cannot enter an allies area");
        }

        if (DeckTypes.IsInPlay(ally.Area.Type))
        {
            throw new RulesNotImplementedException(
                $"ally {ally.ObjectId} is already in play and cannot be put into play again");
        }

        if (facts.RequiredForm(ally.FaceId) is { } form
            && !Forms.In(world, world.Seats[controller], facts, form))
        {
            throw new RulesNotImplementedException(
                $"ally {ally.ObjectId} can only be put into play in {form} form");
        }

        if (Uniqueness.IsBlocked(world, facts, ally, PlayArea.Of(controller)))
        {
            // `rr:unique-icon.4.1`: an attempted put-into-play has no effect.
            return false;
        }

        if (payment is not null && !payment.MatchesPending(ally, controller))
            throw new RulesNotImplementedException("the paid ally changed before entry completed");

        return true;
    }

}
