using Marvel.Protocol;
namespace Marvel.Client;

/// <summary>Private local credentials and a retained exact command; never part of the deterministic campaign archive.</summary>
public sealed record CampaignConnection(string LocalId, string Endpoint, string Label, string? CampaignId,
    string? Capability, ClientSession? Game, EngineRequest? Pending)
{
    /// <summary>Last confirmed manager summary; live authority is refreshed before changes.</summary>
    public CampaignDescriptor? Snapshot { get; init; }

    /// <summary>The server refused this campaign's save; it can only be deleted, never resumed.</summary>
    public bool Refused { get; init; }

    /// <summary>Retains a rejected command's recovery state and records a refused save.</summary>
    public CampaignConnection Rejected(ClientCampaignResult result) => this with
    {
        Pending = result.Pending,
        Refused = Refused || result.Error?.Code == "save_incompatible",
    };

    /// <summary>The first saved campaign worth reopening automatically, or -1 when none is.</summary>
    public static int FirstResumable(IReadOnlyList<CampaignConnection> saved)
    {
        ArgumentNullException.ThrowIfNull(saved);
        for (int index = 0; index < saved.Count; index++)
            if (!saved[index].Refused) return index;
        return -1;
    }
}
