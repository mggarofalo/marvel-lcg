using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityRepeatedStatusTrace;

namespace Marvel.Cards.Run;

/// <summary>Applies status limits and status-dependent departures to a private trace.</summary>
internal sealed class DamageTraceStatuses(DamageTraceState state, AbilityAdmissionScope cast)
{
    private int CurrentTough(int cardId) => state.Tough.GetValueOrDefault(
        cardId, Statuses.Count(cast.World, cast.World.Cards[cardId], Statuses.Tough));

    internal void GrantStatus(int cardId, string status)
    {
        if (state.Discarded.Contains(cardId))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' would give a status to a card that is not there");
        }
        var card = cast.World.Cards[cardId];
        if (status == Statuses.Tough) GrantTough(card);
        else GrantLimitedStatus(card, status);
    }

    private void GrantTough(Card card)
    {
        if (CurrentTough(card.ObjectId) >= 1) return;
        SetTough(card, 1);
    }

    internal void SetTough(Card card, int count)
    {
        int live = Statuses.Count(cast.World, card, Statuses.Tough);
        if (count == live) state.Tough.Remove(card.ObjectId);
        else state.Tough[card.ObjectId] = count;
        TraceSetStatusCount(
            card, Statuses.Tough, count, cast, state.StatusCounts, state.StatusChanges);
    }

    private void GrantLimitedStatus(Card card, string status)
    {
        var key = (card.ObjectId, status);
        int current = state.StatusCounts.GetValueOrDefault(
            key, Statuses.Count(cast.World, card, status));
        int limit = TraceStatusLimit(card, status, cast, state.Discarded, state.Modifiers);
        if (current >= limit) return;
        int changed = current + 1;
        TraceSetStatusCount(
            card, status, changed, cast, state.StatusCounts, state.StatusChanges);
        if (TraceStatusMakesVulnerable(
            card, status, changed, limit, cast, state.Discarded, state.Modifiers))
        {
            new DamageTraceDepartures(state, cast).LeavePlay(card.ObjectId);
        }
    }

}
