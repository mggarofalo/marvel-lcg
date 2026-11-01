namespace Marvel.Godot;

/// <summary>What the table shows about separated game areas, built only from published facts.</summary>
/// <param name="Progress">The engine's progress sentence, including the stored-Dominion count.</param>
/// <param name="SharedPiles">The engine's note that every area shares the encounter deck and discard.</param>
/// <param name="Rules">The engine's whole-table notes for the separated table.</param>
/// <param name="Columns">The central column, then each game area in engine order; empty while the table is whole.</param>
/// <param name="OutOfReach">Cards the inspected seat's cards cannot affect.</param>
internal sealed record GameAreaTable(
    string Progress,
    string? SharedPiles,
    IReadOnlyList<string> Rules,
    IReadOnlyList<GameAreaColumn> Columns,
    IReadOnlySet<int> OutOfReach)
{
    /// <summary>Whether the table is divided into more than one column.</summary>
    internal bool Separated => Columns.Count > 0;
}
