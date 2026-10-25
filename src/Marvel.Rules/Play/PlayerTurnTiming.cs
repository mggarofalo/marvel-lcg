namespace Marvel.Rules.Play;

/// <summary>Timing specific to an individual player's turn.</summary>
public static class PlayerTurnTiming
{
    /// <summary>The active player begins their turn before taking actions.</summary>
    public const string Begins = "WhenPlayerTurnBegins";

    /// <summary>The active player finishes their turn.</summary>
    public const string Ends = "WhenPlayerTurnEnds";
}
