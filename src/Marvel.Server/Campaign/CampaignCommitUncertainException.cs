namespace Marvel.Server;

/// <summary>A publication may have reached disk; the caller must freeze and reload instead of claiming rollback.</summary>
public sealed class CampaignCommitUncertainException : IOException
{
    /// <summary>Records an uncertain manifest publication without guessing which generation survived.</summary>
    public CampaignCommitUncertainException(Exception inner) : base("campaign publication requires recovery", inner) { }
}
