namespace Marvel.Rules.Setup;

/// <summary>The supported table size from the Core Learn to Play overview.</summary>
public static class PlayerCountRules
{
    /// <summary>The smallest legal table.</summary>
    public const int Minimum = 1;
    /// <summary>The largest legal table.</summary>
    public const int Maximum = 4;

    /// <summary>Rejects a table outside the game's one-to-four-player boundary.</summary>
    public static void Validate(int players)
    {
        // Core Learn to Play: "a cooperative card game for one to four players".
        ArgumentOutOfRangeException.ThrowIfLessThan(players, Minimum);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(players, Maximum);
    }
}
