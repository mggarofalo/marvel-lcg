namespace Marvel.Protocol;

/// <summary>Private authority for resuming a newly imported campaign attempt.</summary>
public sealed record CampaignGameAccess(string GameId, string Capability);
