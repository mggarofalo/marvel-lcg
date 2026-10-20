using System.Collections.Immutable;

namespace Marvel.Rules.Campaign;

/// <summary>The campaign-relevant terminal result; clients cannot submit these engine facts.</summary>
public sealed record CampaignAttemptOutcome(
    bool Won,
    ImmutableArray<CampaignSeatOutcome> Seats,
    ImmutableArray<string> ExperimentalWeapons,
    int DelayCounters,
    bool HydraPrisonInPlay,
    ImmutableArray<string> ImprisonedAllyFaces);
