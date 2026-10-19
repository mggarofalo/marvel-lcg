using Marvel.Rules.Campaign;
namespace Marvel.Protocol;

/// <summary>Campaign-owner knowledge and the next rules-owned choice, without hidden live-game state.</summary>
public sealed record CampaignDescriptor(string Id, long Revision, CampaignState State,
    CampaignRewardChoice? Reward, string? ActiveGameId);
