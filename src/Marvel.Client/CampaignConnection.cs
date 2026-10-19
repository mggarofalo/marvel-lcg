using Marvel.Protocol;
namespace Marvel.Client;

/// <summary>Private local credentials and a retained exact command; never part of the deterministic campaign archive.</summary>
public sealed record CampaignConnection(string LocalId, string Endpoint, string Label, string? CampaignId,
    string? Capability, ClientSession? Game, EngineRequest? Pending);
