using Marvel.Session;
namespace Marvel.Server;

/// <summary>Routes every campaign-owned session commit into its campaign's single aggregate publication.</summary>
internal sealed class CampaignSessionStore : IContentUpgradeStore
{
    private readonly ISessionStore ordinary;
    private readonly CampaignHost owner;
    internal CampaignSessionStore(ISessionStore ordinary, CampaignHost owner)
    { this.ordinary = ordinary; this.owner = owner; }

    public IReadOnlyList<StoredSession> Load() => LoadForRestore().Where(item => item.Session is not null)
        .Select(item => item.Session!).ToArray();

    public IReadOnlyList<SessionLoadResult> LoadForRestore() => [.. ordinary.LoadForRestore(),
        .. owner.Campaigns.Values.SelectMany(campaign => campaign.Save.Attempts.Select(attempt =>
            new SessionLoadResult(new StoredSession(attempt, campaign.Authorities.Single(
                item => item.StorageId == attempt.Session.StorageId).Authorities), attempt.Session.StorageId, null)))];

    public string? Commit(StoredSession session)
    {
        if (session.Save.Setup.Campaign is null) return ordinary.Commit(session);
        StoredCampaign? campaign = owner.Starting ?? owner.Campaigns.Values.FirstOrDefault(candidate =>
            candidate.Save.Attempts.Any(attempt => attempt.Session.StorageId == session.Save.Session.StorageId));
        if (campaign is null) throw new SessionSaveException("campaign attempt has no owning aggregate");
        StoredCampaign proposed = owner.Starting is not null
            ? CampaignTransactions.Admit(campaign, session, owner.Factory)
            : CampaignTransactions.Update(campaign, session, owner.Factory);
        if (owner.StartingRequest is { } request)
            proposed = CampaignCommands.AddReceipt(proposed, request, session.Save.Session.Label);
        return owner.Publish(proposed);
    }

    public string? CommitContentUpgrade(StoredSession previous, StoredSession candidate, string? generation)
    {
        if (previous.Save.Setup.Campaign is not null || ordinary is not IContentUpgradeStore upgrades)
            throw new SessionSaveException("campaign content upgrades require complete aggregate verification");
        return upgrades.CommitContentUpgrade(previous, candidate, generation);
    }
}
