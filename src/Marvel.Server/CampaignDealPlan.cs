using Marvel.Rules.Campaign;
using Marvel.Rules.State;

namespace Marvel.Server;

/// <summary>Deterministic physical allocation and campaign identities for one attempt.</summary>
internal sealed record CampaignDealPlan(
    IReadOnlyList<CardBlueprint> Blueprints,
    IReadOnlyList<CampaignCardBinding> Bindings,
    IReadOnlyList<int> ExperimentalWeapons);
