namespace Marvel.Server;

/// <summary>An independent restore candidate; rejected archives remain intact for explicit recovery.</summary>
public sealed record CampaignLoadResult(string StorageId, StoredCampaign? Campaign, string? Error);
