namespace Marvel.Server;

/// <summary>Explains the latest edit boundary without exposing its private frontier signal.</summary>
// The protocol chooses these stable numeric values; they are not game rules.
public enum HistoryUndoStatus
{
    /// <summary>No more specific explanation is supplied.</summary>
    Unavailable = 0,
    /// <summary>The latest completed action is editable by this capability.</summary>
    Available = 1,
    /// <summary>No completed action precedes this cursor.</summary>
    NoHistory = 2,
    /// <summary>An indivisible action still has dependent decisions.</summary>
    ActionInProgress = 3,
    /// <summary>The action crosses the information or randomness boundary.</summary>
    ProtectedHistory = 4,
    /// <summary>The action includes decisions outside this capability's edit authority.</summary>
    OtherPlayer = 5,
}
