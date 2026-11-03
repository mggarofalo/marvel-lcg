namespace Marvel.Rules.Play;

/// <summary>Where a batched minion entry is. The division into stages is an engine choice.</summary>
public enum MinionEntryBatchStage
{
    /// <summary>Discard the next card and put it into play if it is a minion.</summary>
    Discard,
    /// <summary>The first player chooses among players tied for the fewest engaged minions.</summary>
    ChooseEngagedPlayer,
    /// <summary>Resolve the batch's Quickstrike and Teamwork activations.</summary>
    Keywords,
    /// <summary>Open each entry's deferred response window.</summary>
    Responses,
}
