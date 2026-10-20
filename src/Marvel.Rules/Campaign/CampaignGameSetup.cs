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
    int AdditionalThreat)
{
    /// <summary>Allies permanently removed by campaign instructions, including signature allies.</summary>
    public ImmutableArray<string> BannedAllyFaces { get; init; } = [];
}
