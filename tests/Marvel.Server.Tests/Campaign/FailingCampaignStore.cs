namespace Marvel.Server.Tests;

internal sealed class FailingCampaignStore : ICampaignStore
{
    internal MemoryCampaignStore Inner { get; } = new();
    internal bool Fail { get; set; }
    internal bool PublishThenFail { get; set; }
    public IReadOnlyList<CampaignLoadResult> Load() => Inner.Load();
    public IReadOnlyList<CampaignDeletion> Deletions() => Inner.Deletions();
    public void Delete(CampaignDeletion deletion)
    {
        if (!Fail) { Inner.Delete(deletion); return; }
        if (!PublishThenFail) throw new IOException("injected before deletion");
        Inner.Delete(deletion);
        throw new CampaignCommitUncertainException(new IOException("injected after deletion"));
    }
    public string Commit(StoredCampaign campaign)
    {
        if (!Fail) return Inner.Commit(campaign);
        if (!PublishThenFail) throw new IOException("injected before publication");
        _ = Inner.Commit(campaign);
        throw new CampaignCommitUncertainException(new IOException("injected after publication"));
    }
}
