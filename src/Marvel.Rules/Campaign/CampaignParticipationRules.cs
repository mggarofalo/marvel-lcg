namespace Marvel.Rules.Campaign;

/// <summary>Keeps the campaign roster fixed while permitting defeated expert players to sit out an attempt.</summary>
public static class CampaignParticipationRules
{
    /// <summary>Chooses whether a defeated roster seat attempts recovery during the next scenario setup.</summary>
    public static CampaignState Select(CampaignState state, int seat, bool participating)
    {
        CampaignRules.RequirePhase(state, CampaignPhase.Preparing);
        CampaignRules.RequireSeat(state, seat);
        var player = state.Players[seat];
        // pack:mc10:persistent-damage: defeated players "can rejoin their teammates for the next scenario" by recovery.
        if (!state.Expert || !player.RequiresRecovery)
            throw new InvalidOperationException("only a defeated expert campaign player may change participation");
        return state with { Players = state.Players.SetItem(seat, player with { Participating = participating }) };
    }
}
