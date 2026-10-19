namespace Marvel.Protocol;

/// <summary>Owner capability authorizing campaign-specific deck construction exceptions.</summary>
public sealed record CampaignDeckAccess(string CampaignId, string Capability);
