using System.Collections.Immutable;

namespace Marvel.Rules.Campaign;

/// <summary>The complete immutable campaign state, derived from accepted engine transitions.</summary>
public sealed record CampaignState(
    string Product,
    uint Seed,
    bool Expert,
    CampaignStage Stage,
    CampaignPhase Phase,
    int Attempt,
    bool ExpertScenario,
    ImmutableArray<CampaignPlayerState> Players,
    ImmutableArray<string> RemovedCardKeys,
    ImmutableArray<string> BannedAllyFaces,
    ImmutableArray<string> ExperimentalWeapons,
    int DelayCounters,
    ImmutableArray<int> RewardSeats,
    CampaignAttemptOutcome? PendingOutcome);
