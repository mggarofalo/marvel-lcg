namespace Marvel.Server;

/// <summary>Publishes campaign state, all attempts, and authorities through one commit boundary.</summary>
public interface ICampaignStore
{
    /// <summary>Loads independent candidates without silently falling back to older campaign state.</summary>
    IReadOnlyList<CampaignLoadResult> Load();
    /// <summary>Returns only after the complete aggregate is durable.</summary>
    string Commit(StoredCampaign campaign);
    /// <summary>Lists permanent revocations retained for exact request recovery.</summary>
    IReadOnlyList<CampaignDeletion> Deletions();
    /// <summary>Publishes revocation before removing all saved campaign generations.</summary>
    void Delete(CampaignDeletion deletion);
}
