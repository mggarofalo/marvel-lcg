namespace Marvel.Server;

/// <summary>One independently loadable session, or why it is withheld or refused.</summary>
public sealed record SessionLoadResult(
    StoredSession? Session,
    string? StorageId,
    string? ErrorCode,
    string? Generation = null)
{
    /// <summary>A save under another schema whose authority still names its holders.</summary>
    public RefusedSchema? Refused { get; init; }
}
