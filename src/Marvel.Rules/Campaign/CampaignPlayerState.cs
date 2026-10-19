using System.Collections.Immutable;
using Marvel.Rules.Setup;

namespace Marvel.Rules.Campaign;

/// <summary>A fixed campaign participant and the durable changes to their deck.</summary>
public sealed record CampaignPlayerState(
    string Hero,
    PlayerDeckRecipe Deck,
    int BaseHitPoints,
    int? RecordedHitPoints,
    bool RequiresRecovery,
    ImmutableArray<CampaignCard> Cards,
    ImmutableArray<CampaignCard> AvailableObligations,
    bool EngagedWithEnemy);
