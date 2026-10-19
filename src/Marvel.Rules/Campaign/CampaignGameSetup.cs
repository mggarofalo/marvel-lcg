using System.Collections.Immutable;

namespace Marvel.Rules.Campaign;

/// <summary>Complete immutable campaign input to a single attempt, persisted with session setup.</summary>
public sealed record CampaignGameSetup(
    string Product,
    bool ExpertCampaign,
    CampaignStage Stage,
    int Attempt,
    ImmutableArray<CampaignSeatSetup> Seats,
    ImmutableArray<string> ExperimentalWeapons,
    int AdditionalThreat);
