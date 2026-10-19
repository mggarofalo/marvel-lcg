using System.Collections.Immutable;

namespace Marvel.Rules.Campaign;

/// <summary>Campaign-only deterministic setup instructions for one fixed seat.</summary>
public sealed record CampaignSeatSetup(
    int Seat,
    ImmutableArray<CampaignCard> Cards,
    int? RecordedHitPoints,
    bool RequiresRecovery,
    ImmutableArray<CampaignCard> AvailableObligations,
    bool DealEncounterCard);
