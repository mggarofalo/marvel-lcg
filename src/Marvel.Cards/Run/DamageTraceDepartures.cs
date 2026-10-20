using Marvel.Rules.Play;
using static Marvel.Cards.Run.AbilityPowerProjection;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityRepeatedStatusTrace;
using static Marvel.Cards.Run.AbilityRepeatedCharacteristicTrace;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityInitiation;

namespace Marvel.Cards.Run;

/// <summary>Settles departures and player elimination in a private repeated-damage trace.</summary>
internal sealed class DamageTraceDepartures(DamageTraceState state, AbilityAdmissionScope cast)
{
    private long Current(int cardId) => state.Damage.GetValueOrDefault(cardId, cast.World.Cards[cardId].Damage);
    private long HealthBonus(int cardId) => state.Health.GetValueOrDefault(cardId);

    internal int ResolveIdentityDefeat(Card card, long tracedHealth)
    {
        int player = PlayerForIdentity(card.ObjectId);
        if (player < 0 || Current(card.ObjectId) < tracedHealth) return state.FirstPlayer;
        ApplyElimination(player);
        return player == state.FirstPlayer ? AdvanceFirstPlayer() : state.FirstPlayer;
    }

    private int PlayerForIdentity(int cardId) => cast.World.Seats
        .Select((seat, player) => (seat, player))
        .Where(pair => pair.seat.IdentityCard.ObjectId == cardId)
        .Select(pair => pair.player)
        .DefaultIfEmpty(-1)
        .First();

    private void ApplyElimination(int player)
    {
        var plan = PlanTracePlayerElimination(
            player, cast, state.Discarded, state.Engagement);
        foreach (int relocated in plan.RelocatedCards)
        {
            state.Engagement[relocated] = plan.NextPlayer!.Value;
        }
        foreach (int leaving in plan.Leaving)
        {
            DiscardCard(leaving);
        }
    }

    private int AdvanceFirstPlayer()
    {
        for (int offset = 1; offset < cast.World.Seats.Count; offset++)
        {
            int candidate = (state.FirstPlayer + offset) % cast.World.Seats.Count;
            var identity = cast.World.Seats[candidate].IdentityCard;
            long candidateHealth = SaturatingAdd(
                TraceHealth(identity, state.Discarded, state.Threat, cast),
                HealthBonus(identity.ObjectId));
            if (Current(identity.ObjectId) >= candidateHealth) continue;
            return candidate;
        }
        return state.FirstPlayer;
    }

    internal void LeavePlay(int cardId)
    {
        foreach (int leaving in LeavingTree(cardId))
        {
            DiscardCard(leaving);
        }
    }

    private List<int> LeavingTree(int cardId)
    {
        var leaving = new List<int> { cardId };
        var pending = new Stack<Card>(ChildrenOf(cardId).Reverse());
        var seen = new HashSet<int> { cardId };
        while (pending.TryPop(out var hosted))
        {
            RefuseInvalidAttachment(hosted, seen, cardId);
            leaving.Add(hosted.ObjectId);
            foreach (var child in ChildrenOf(hosted.ObjectId).Reverse())
            {
                pending.Push(child);
            }
        }
        return leaving;
    }

    private IEnumerable<Card> ChildrenOf(int host) => cast.World.Areas
        .Where(area => area.Host == host)
        .SelectMany(area => area.Cards);

    private void RefuseInvalidAttachment(
        Card hosted, HashSet<int> seen, int root)
    {
        if (!seen.Add(hosted.ObjectId))
        {
            throw new RulesNotImplementedException(
                $"attachment {hosted.ObjectId} forms a hosting cycle");
        }
        if (StateFields.Modified(
                cast.World, hosted, "permanent",
                cast.World.Facts, cast.World.Players) <= 0) return;
        // Match Discard.Attachments' complete-tree preflight so eligibility
        // refuses before an action cost can mutate.
        throw new RulesNotImplementedException(
            $"permanent attachment {hosted.ObjectId} lost host {root}, "
            + "and rr:permanent.5 is not implemented");
    }

    private void DiscardCard(int cardId)
    {
        state.Discarded.Add(cardId);
        if (Statuses.Count(
                cast.World, cast.World.Cards[cardId], Statuses.Tough) > 0)
        {
            state.Tough[cardId] = 0;
        }
        else state.Tough.Remove(cardId);
        TraceStatusesLeave(cardId, cast, state.StatusCounts, state.StatusChanges);
        state.Engagement.Remove(cardId);
    }
}
