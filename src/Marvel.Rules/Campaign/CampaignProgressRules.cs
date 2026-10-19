using System.Collections.Immutable;

namespace Marvel.Rules.Campaign;

/// <summary>Applies verified game effects without trusting a client campaign log.</summary>
public static class CampaignProgressRules
{
    /// <summary>Reconciles cumulative current-attempt facts, idempotently retaining campaign removals.</summary>
    public static CampaignState Apply(CampaignState state, CampaignGameProgress progress)
    {
        CampaignRules.RequirePhase(state, CampaignPhase.Playing);
        ArgumentNullException.ThrowIfNull(progress);
        foreach (var assignment in progress.Obligations)
            state = AssignObligation(state, assignment);
        foreach (var removal in progress.Removals)
            state = RemoveCard(state, removal);
        return state;
    }

    private static CampaignState AssignObligation(CampaignState state, CampaignObligationAssignment assignment)
    {
        CampaignRules.RequireSeat(state, assignment.Seat);
        CampaignPlayerState player = state.Players[assignment.Seat];
        if (!state.Expert || assignment.Card.Role != CampaignCardRole.Obligation)
            throw new ArgumentException("campaign obligation assignment is not admitted", nameof(assignment));
        if (player.Cards.Contains(assignment.Card)) return state;
        if (!player.AvailableObligations.Contains(assignment.Card))
            throw new ArgumentException("obligation must come from this seat's remaining physical pool", nameof(assignment));
        player = player with
        {
            Cards = player.Cards.Add(assignment.Card),
            AvailableObligations = player.AvailableObligations.Remove(assignment.Card),
            RequiresRecovery = false,
        };
        return state with { Players = state.Players.SetItem(assignment.Seat, player) };
    }

    private static CampaignState RemoveCard(CampaignState state, CampaignCardRemoval removal)
    {
        CampaignRules.RequireSeat(state, removal.Seat);
        if (state.RemovedCardKeys.Contains(removal.CardKey)) return state;
        CampaignPlayerState player = state.Players[removal.Seat];
        CampaignCard? card = player.Cards.SingleOrDefault(card => card.Key == removal.CardKey);
        if (card is null)
            throw new ArgumentException("campaign removal names no owned card", nameof(removal));
        return state with
        {
            Players = state.Players.SetItem(removal.Seat, player with { Cards = player.Cards.Remove(card) }),
            RemovedCardKeys = state.RemovedCardKeys.Add(card.Key),
        };
    }
}
