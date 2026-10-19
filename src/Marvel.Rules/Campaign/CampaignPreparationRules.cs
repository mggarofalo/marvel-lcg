using System.Collections.Immutable;
namespace Marvel.Rules.Campaign;

/// <summary>Explains rules-owned barriers to beginning the prepared campaign attempt.</summary>
public static class CampaignPreparationRules
{
    /// <summary>Returns stable, player-facing corrections without allocating a game or drawing obligations.</summary>
    public static ImmutableArray<string> Problems(CampaignState state)
    {
        CampaignRules.RequirePhase(state, CampaignPhase.Preparing);
        var problems = ImmutableArray.CreateBuilder<string>();
        if (!state.Players.Any(player => player.Participating)) problems.Add("Choose at least one player for this attempt.");
        for (int seat = 0; seat < state.Players.Length; seat++)
        {
            var player = state.Players[seat];
            if (player.Participating && player.RequiresRecovery && player.AvailableObligations.IsEmpty)
                problems.Add($"Player {seat + 1} has no recovery obligations left and must sit out this attempt.");
        }
        return problems.ToImmutable();
    }
}
