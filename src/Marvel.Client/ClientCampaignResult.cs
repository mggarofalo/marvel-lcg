using Marvel.Protocol;
namespace Marvel.Client;

/// <summary>A validated campaign response and any retained uncertain command.</summary>
public sealed record ClientCampaignResult(EngineResponse? Response, ClientEntryResult? Entry,
    ClientStartupError? Error, EngineRequest? Pending)
{
    /// <summary>Whether the service returned complete campaign authority.</summary>
    public bool Succeeded => Error is null && Response is not null
        && (Response.Campaign is not null || Response.CampaignDeleted);
}
