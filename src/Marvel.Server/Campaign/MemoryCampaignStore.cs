namespace Marvel.Server;

/// <summary>In-memory campaign persistence for isolated deterministic tests.</summary>
public sealed class MemoryCampaignStore : ICampaignStore
{
    private readonly Dictionary<string, StoredCampaign> campaigns = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CampaignDeletion> deleted = new(StringComparer.Ordinal);
    /// <inheritdoc />
    public IReadOnlyList<CampaignDeletion> Deletions() => deleted.Values.ToArray();
    /// <inheritdoc />
    public void Delete(CampaignDeletion deletion)
    {
        deleted[deletion.StorageId] = deletion;
        campaigns.Remove(deletion.StorageId);
    }
    /// <inheritdoc />
    public IReadOnlyList<CampaignLoadResult> Load() => campaigns.OrderBy(pair => pair.Key, StringComparer.Ordinal)
        .Select(pair => new CampaignLoadResult(pair.Key, pair.Value, null)).ToArray();
    /// <inheritdoc />
    public string Commit(StoredCampaign campaign)
    {
        FileCampaignStore.Validate(campaign);
        if (deleted.ContainsKey(campaign.Save.Identity.StorageId))
            throw new Marvel.Session.SessionSaveException("campaign was permanently deleted");
        campaigns[campaign.Save.Identity.StorageId] = campaign;
        return campaign.Save.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
