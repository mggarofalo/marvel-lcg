namespace Marvel.Server;

/// <summary>An authenticated command's durable idempotency proof, separate from deterministic game history.</summary>
public sealed record CampaignReceipt(string RequestId, string Fingerprint, long Revision, string? GameId = null);
