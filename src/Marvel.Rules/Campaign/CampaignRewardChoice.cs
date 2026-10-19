using System.Collections.Immutable;

namespace Marvel.Rules.Campaign;

/// <summary>An exact seat-owned campaign choice, with complete currently available options.</summary>
public sealed record CampaignRewardChoice(
    int Seat,
    CampaignRewardKind Kind,
    bool Optional,
    ImmutableArray<CampaignCard> Options);
