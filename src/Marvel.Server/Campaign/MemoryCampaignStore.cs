namespace Marvel.Server;

/// <summary>In-memory campaign persistence for isolated deterministic tests.</summary>
public sealed class MemoryCampaignStore : ICampaignStore
{
    private readonly Dictionary<string, StoredCampaign> campaigns = new(StringComparer.Ordinal);
    /// <inheritdoc />
    public IReadOnlyList<CampaignLoadResult> Load() => campaigns.OrderBy(pair => pair.Key, StringComparer.Ordinal)
        .Select(pair => new CampaignLoadResult(pair.Key, pair.Value, null)).ToArray();
    /// <inheritdoc />
    public string Commit(StoredCampaign campaign)
    {
        FileCampaignStore.Validate(campaign);
        campaigns[campaign.Save.Identity.StorageId] = campaign;
        return campaign.Save.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
