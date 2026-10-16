namespace Marvel.Rules.Play;

/// <summary>The rule-owned operation that invokes or resumes card text.</summary>
public enum AbilityContinuationStage
{
    /// <summary>Resume after a completed rule procedure.</summary>
    Resume,
    /// <summary>Ask or answer an authored choice.</summary>
    Choose,
    /// <summary>The first player orders all resolving players.</summary>
    OrderPlayers,
    /// <summary>Resolve the effect for one ordered player.</summary>
    ResolvePlayer,
    /// <summary>Initiate one card's Special ability.</summary>
    Special,
}
