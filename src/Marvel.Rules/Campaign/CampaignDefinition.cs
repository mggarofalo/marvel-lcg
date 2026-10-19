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
    ImmutableArray<string> CaptiveAllyFaces)
{
    /// <summary>The printed prison whose presence and hosted allies determine the Zola result.</summary>
    public string HydraPrisonFace { get; init; } = string.Empty;

    /// <summary>The engine-owned counter field authored for Absorbing Man's delay counters.</summary>
    public string DelayCounterField { get; init; } = "k_delay";
}
