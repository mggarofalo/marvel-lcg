namespace Marvel.Rules.Campaign;

/// <summary>Campaign facts that survive a card changing zones or leaving play.</summary>
public sealed record CampaignCardProgress(
    CampaignCardBinding Binding, bool Removed, bool Rescued, bool Assigned);
