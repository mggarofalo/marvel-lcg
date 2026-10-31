using Marvel.Session;

namespace Marvel.Server;

/// <summary>A campaign archive this runtime refuses to load, kept so its owner can still delete it.</summary>
internal sealed record RefusedCampaign(StoredCampaign Campaign, string Category)
{
    internal bool OwnedBy(string? capability) =>
        capability is not null && Campaign.OwnerVerifier == SessionAuthorityRegistry.Verifier(capability);

    internal void RefuseAttempts(RefusedSaves games)
    {
        foreach (SessionSave attempt in Campaign.Save.Attempts)
        {
            if (Campaign.Authorities.FirstOrDefault(item => item.StorageId == attempt.Session.StorageId) is { } granted)
                games.Refuse(attempt.Session.Label, granted.Authorities, Category);
        }
    }
}
