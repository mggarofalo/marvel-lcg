namespace Marvel.Server;

/// <summary>Minimal durable revocation and retry receipt, containing no campaign or game state.</summary>
public sealed record CampaignDeletion(string StorageId, string OwnerVerifier, string RequestId,
    string Fingerprint, string EntryVerifier, string[] GameIds, string[] GameEntryVerifiers)
{
    /// <summary>The version of the operational revocation format.</summary>
    public int Schema { get; init; } = 1;
}
