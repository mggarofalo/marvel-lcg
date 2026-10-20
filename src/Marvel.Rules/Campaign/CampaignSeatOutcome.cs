using System.Collections.Immutable;

namespace Marvel.Rules.Campaign;

/// <summary>Terminal facts required by campaign instructions, obtained from the verified game.</summary>
public sealed record CampaignSeatOutcome(
    int Seat,
    bool Defeated,
    bool HeroForm,
    int RemainingHitPoints,
    int BaseHitPoints,
    bool EngagedWithEnemy,
    ImmutableArray<CampaignCard> RescuedAllies);
