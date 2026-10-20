namespace Marvel.Rules.Campaign;

/// <summary>The authoritative campaign continuation, separate from a game's phase.</summary>
public enum CampaignPhase
{
    /// <summary>Decks and independent scenario mode may be selected.</summary>
    Preparing,
    /// <summary>An attempt has been opened and its game owns the next decision.</summary>
    Playing,
    /// <summary>Ordered victory choices remain.</summary>
    Rewards,
    /// <summary>A lost attempt may be reset.</summary>
    Retry,
    /// <summary>All five scenarios were won.</summary>
    Complete,
    /// <summary>The expert campaign ended in defeat.</summary>
    Failed,
}
