namespace Marvel.Server;

/// <summary>Publishes a complete verified campaign migration with a retained predecessor archive.</summary>
public interface IContentUpgradeCampaignStore : ICampaignStore
{
    /// <summary>Preserves the exact current archive and rejects stale migration candidates.</summary>
    string CommitContentUpgrade(StoredCampaign previous, StoredCampaign candidate);
}
