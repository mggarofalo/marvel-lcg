using System.Collections.Immutable;

namespace Marvel.Rules.Campaign;

/// <summary>Admitted product composition supplied by content, never inferred from card names.</summary>
public sealed record CampaignDefinition(
    string Product,
    ImmutableArray<CampaignScenario> Scenarios,
    ImmutableArray<CampaignCard> Tech,
    ImmutableArray<CampaignCondition> Conditions,
    ImmutableArray<string> ObligationFaces,
    ImmutableArray<string> ExperimentalWeaponFaces,
    ImmutableArray<string> CaptiveAllyFaces);
