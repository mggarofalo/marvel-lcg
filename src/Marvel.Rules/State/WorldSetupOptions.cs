using Marvel.Rules.Campaign;

namespace Marvel.Rules.State;

/// <summary>Immutable game mode and campaign bindings available before setup abilities execute.</summary>
public sealed record WorldSetupOptions(
    bool Expert = false,
    CampaignGameSetup? Campaign = null,
    IReadOnlyList<CampaignCardBinding>? CampaignCards = null);
