namespace Marvel.Godot;

/// <summary>A typed fact about one game-area column, shown as a word so it never depends on color.</summary>
internal enum GameAreaMarker
{
    /// <summary>The inspected seat plays in this area.</summary>
    Yours,

    /// <summary>The engine's villain-phase focus is resolving in this area.</summary>
    Acting,

    /// <summary>The area holds an eliminated player's components until they depart.</summary>
    Retained,

    /// <summary>A Kang (II) group leaves this area at the end of the phase.</summary>
    Departing,

    /// <summary>The inspected seat's cards cannot affect this area's cards.</summary>
    OutOfReach,
}
