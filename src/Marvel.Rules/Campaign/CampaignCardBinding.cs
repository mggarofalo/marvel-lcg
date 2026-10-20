namespace Marvel.Rules.Campaign;

/// <summary>Maps one deterministic game object to its persistent campaign identity.</summary>
public sealed record CampaignCardBinding(int ObjectId, int Seat, CampaignCard Card);
