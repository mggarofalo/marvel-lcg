using Marvel.Protocol;
namespace Marvel.Server;

/// <summary>Owner-authorized administrative choices, separate from replayed game rules.</summary>
internal static class CampaignManagement
{
    internal static EngineResponse? Read(CampaignHost host, EngineRequest request, StoredCampaign campaign)
    {
        return request.Campaign!.Action switch
        {
            "sync" => CampaignHost.Success(request) with { Campaign = host.Describe(campaign) },
            "export" => CampaignHost.Success(request) with { Campaign = host.Describe(campaign),
                CampaignTransfer = CampaignTransferCodec.Export(campaign.Save) },
            _ => null,
        };
    }

    internal static StoredCampaign? Change(StoredCampaign campaign, CampaignQuery query)
    {
        return query.Action switch
        {
            "rename" => campaign with { Save = campaign.Save with
                { Identity = campaign.Save.Identity with { Label = Name(query.Name) }, Revision = checked(campaign.Save.Revision + 1) } },
            "archive" or "restore" => campaign with { Archived = query.Action == "archive",
                Save = campaign.Save with { Revision = checked(campaign.Save.Revision + 1) } },
            _ => null,
        };
    }

    internal static string Name(string? value)
    {
        string name = value?.Trim() ?? "";
        if (name.Length is < 1 or > 128 || name.Any(char.IsControl))
            throw new ArgumentException("campaign names require 1–128 printable characters");
        return name;
    }

    internal static string CopyName(string value)
    {
        string name = Name(value);
        return name[..Math.Min(name.Length, 121)] + " (copy)";
    }

    internal static EngineResponse Deleted(EngineRequest request, CampaignDeletion deletion)
    {
        if (request.Capability is null || deletion.OwnerVerifier != SessionAuthorityRegistry.Verifier(request.Capability))
            return CampaignHost.Failure(request, "campaign_not_found", "The campaign owner capability is not valid.");
        return request.Campaign!.Action == "delete" && request.RequestId == deletion.RequestId
            && EntryReceipts.Fingerprint(request) == deletion.Fingerprint
            ? CampaignHost.Success(request) with { CampaignDeleted = true }
            : CampaignHost.Failure(request, "campaign_deleted", "This campaign has been permanently deleted.");
    }
}
