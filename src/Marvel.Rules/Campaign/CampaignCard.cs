namespace Marvel.Rules.Campaign;

/// <summary>A stable campaign physical instance; its identity is independent of game object ids.</summary>
public sealed record CampaignCard(string Key, string Face, CampaignCardRole Role);
