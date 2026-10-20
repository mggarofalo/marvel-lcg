namespace Marvel.Server;

/// <summary>Publishes a verified content migration while preserving its exact predecessor archive.</summary>
public interface IContentUpgradeStore : ISessionStore
{
    /// <summary>Rejects a changed predecessor and preserves it before atomically publishing the candidate.</summary>
    string? CommitContentUpgrade(StoredSession previous, StoredSession candidate, string? generation);
}
