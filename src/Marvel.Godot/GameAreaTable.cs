namespace Marvel.Godot;

/// <summary>What the table shows about separated game areas, built only from published facts.</summary>
/// <param name="Tracker">Progress toward winning: the Kang faced now, the Kang (II) still in play and the stored Dominions.</param>
/// <param name="Rules">The rules the engine says apply while the table is separated, worded, once each.</param>
/// <param name="Columns">The columns in display order, the central one between the game areas; empty while the table is whole.</param>
/// <param name="OutOfReach">Cards the inspected seat's cards cannot affect.</param>
internal sealed record GameAreaTable(
    TrackerState Tracker,
    IReadOnlyList<string> Rules,
    IReadOnlyList<GameAreaColumn> Columns,
    IReadOnlySet<int> OutOfReach)
{
    /// <summary>Whether the table is divided into more than one column.</summary>
    internal bool Separated => Columns.Count > 0;
}
