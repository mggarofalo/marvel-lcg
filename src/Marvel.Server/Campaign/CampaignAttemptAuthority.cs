using System.Collections.Immutable;
namespace Marvel.Server;

/// <summary>Protected operational authority associated with one retained attempt.</summary>
public sealed record CampaignAttemptAuthority(string StorageId, ImmutableArray<StoredAuthority> Authorities);
