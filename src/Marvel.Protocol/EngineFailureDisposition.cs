namespace Marvel.Protocol;

/// <summary>The authoritative effect of a failed request on session recovery.</summary>
public enum EngineFailureDisposition
{
    /// <summary>No change committed; the session capability remains usable.</summary>
    Rejected,
    /// <summary>The presented authority no longer identifies an available session.</summary>
    SessionUnavailable,
    /// <summary>The response cannot establish whether a change committed; synchronize before acting.</summary>
    CommitUncertain,
}
