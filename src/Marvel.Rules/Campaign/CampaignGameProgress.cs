using System.Collections.Immutable;

namespace Marvel.Rules.Campaign;

/// <summary>Cumulative campaign effects at the current verified attempt cursor.</summary>
/// <remarks>Repeated inspection returns the same facts without consuming RNG or changing the game.</remarks>
public sealed record CampaignGameProgress(
    ImmutableArray<CampaignCardRemoval> Removals,
    ImmutableArray<CampaignObligationAssignment> Obligations,
    CampaignAttemptOutcome? Outcome);
