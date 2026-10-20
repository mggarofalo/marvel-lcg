using Marvel.Rules.Campaign;
namespace Marvel.Protocol;

/// <summary>Campaign-owner knowledge and the next rules-owned choice, without hidden live-game state.</summary>
public sealed record CampaignDescriptor(string Id, long Revision, CampaignState State,
    CampaignRewardChoice? Reward, string? ActiveGameId)
{
    /// <summary>The durable owner-selected name.</summary>
    public string Name { get; init; } = "";
    /// <summary>Archived campaigns remain readable but cannot be played until restored.</summary>
    public bool Archived { get; init; }
    /// <summary>Printed inventory identities for campaign cards permanently consumed from the log.</summary>
    public IReadOnlyList<CampaignCard> RemovedCards { get; init; } = [];

    /// <summary>Host-authored corrections that must be resolved before beginning an attempt.</summary>
    public IReadOnlyList<string> PreparationProblems { get; init; } = [];
}

