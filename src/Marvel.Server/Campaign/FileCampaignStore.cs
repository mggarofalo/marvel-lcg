using System.Text;
using System.Text.Json;
using Marvel.Session;
namespace Marvel.Server;

/// <summary>Private, bounded, checksummed campaign archives with an exclusive process writer lease.</summary>
public sealed class FileCampaignStore : ICampaignStore, IDisposable
{
    private readonly CampaignArchiveFiles files;
    private readonly CampaignArchiveFiles deleted;
    /// <summary>Acquires exclusive write ownership for the campaign root.</summary>
    public FileCampaignStore(string root)
    {
        files = new CampaignArchiveFiles(root);
        try { deleted = new CampaignArchiveFiles(Path.Combine(root, ".deleted")); }
        catch { files.Dispose(); throw; }
    }

    /// <inheritdoc />
    public IReadOnlyList<CampaignDeletion> Deletions() => deleted.Ids()
        .Select(ReadDeletion).ToArray();

    private CampaignDeletion ReadDeletion(string id)
    {
        var deletion = CampaignSaveJson.ReadDocument<CampaignDeletion>(deleted.Read(id));
        CampaignDeletionValidation.Validate(deletion);
        if (deletion.StorageId != id) throw new SessionSaveException("campaign revocation identity differs from directory");
        return deletion;
    }

    /// <inheritdoc />
    public void Delete(CampaignDeletion deletion)
    {
        CampaignDeletionValidation.Validate(deletion);
        _ = deleted.Commit(deletion.StorageId, Encoding.UTF8.GetBytes(CampaignSaveJson.Canonical(deletion)));
        try { files.Purge(deletion.StorageId); }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        { throw new CampaignCommitUncertainException(failure); }
    }

    /// <inheritdoc />
    public IReadOnlyList<CampaignLoadResult> Load()
    {
        var result = new List<CampaignLoadResult>();
        var revoked = Deletions().Select(item => item.StorageId).ToHashSet(StringComparer.Ordinal);
        foreach (string id in files.Ids())
        {
            if (revoked.Contains(id)) { files.Purge(id); continue; }
            try
            {
                StoredCampaign campaign = CampaignSaveJson.ReadDocument<StoredCampaign>(files.Read(id));
                Validate(campaign);
                if (campaign.Save.Identity.StorageId != id) throw new SessionSaveException("campaign identity differs from directory");
                result.Add(new(id, campaign, null));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or SessionSaveException or JsonException or InvalidOperationException or ArgumentException)
            { result.Add(new(id, null, "campaign_restore_failed")); }
        }
        return result;
    }

    /// <inheritdoc />
    public string Commit(StoredCampaign campaign)
    {
        Validate(campaign);
        if (Deletions().Any(item => item.StorageId == campaign.Save.Identity.StorageId))
            throw new SessionSaveException("campaign was permanently deleted");
        return files.Commit(campaign.Save.Identity.StorageId, Encoding.UTF8.GetBytes(CampaignSaveJson.Canonical(campaign)));
    }

    internal static void Validate(StoredCampaign campaign)
    {
        _ = CampaignSaveJson.Write(campaign.Save);
        if (campaign.OwnerVerifier is not { Length: 64 } || !campaign.OwnerVerifier.All(char.IsAsciiHexDigit)
            || campaign.Entry is null || !EntryReceipts.Valid(campaign.Entry))
            throw new SessionSaveException("campaign owner authority is invalid");
        if (campaign.Authorities.IsDefault || campaign.Receipts.IsDefault || campaign.Receipts.Length > 100000)
            throw new SessionSaveException("campaign operational records are invalid");
        ValidateAuthorities(campaign);
        ValidateReceipts(campaign);
    }

    private static void ValidateAuthorities(StoredCampaign campaign)
    {
        if (campaign.Authorities.Any(item => item is null || item.Authorities.IsDefault))
            throw new SessionSaveException("campaign attempt authority is malformed");
        if (campaign.Authorities.Select(item => item.StorageId).Distinct(StringComparer.Ordinal).Count() != campaign.Authorities.Length)
            throw new SessionSaveException("campaign attempt authority is duplicated");
        if (campaign.Authorities.Length != campaign.Save.Attempts.Length)
            throw new SessionSaveException("campaign attempt authority is incomplete");
        foreach (var attempt in campaign.Save.Attempts)
        {
            var authorities = campaign.Authorities.SingleOrDefault(item => item.StorageId == attempt.Session.StorageId)
                ?? throw new SessionSaveException("campaign attempt authority is absent");
            StoredSessionJson.ValidateLoaded(new(attempt, authorities.Authorities));
        }
    }

    private static void ValidateReceipts(StoredCampaign campaign)
    {
        if (campaign.Receipts.Select(receipt => receipt?.RequestId).Distinct(StringComparer.Ordinal).Count() != campaign.Receipts.Length)
            throw new SessionSaveException("campaign command receipt is duplicated");
        foreach (var receipt in campaign.Receipts)
        {
            if (!ValidReceipt(receipt, campaign.Save.Revision)) throw new SessionSaveException("campaign command receipt is malformed");
            if (receipt.GameId is not null && !campaign.Save.Attempts.Any(attempt => attempt.Session.Label == receipt.GameId))
                throw new SessionSaveException("campaign receipt names an absent attempt");
        }
    }

    private static bool ValidReceipt(CampaignReceipt? receipt, long revision) => receipt is
        { RequestId.Length: > 0 and <= 256, Fingerprint.Length: 64, Revision: >= 0 }
        && receipt.Revision <= revision && receipt.Fingerprint.All(char.IsAsciiHexDigit);

    /// <summary>Releases the exclusive process writer lease.</summary>
    public void Dispose() { deleted.Dispose(); files.Dispose(); }
}
