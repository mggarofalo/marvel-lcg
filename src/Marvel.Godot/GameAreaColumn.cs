namespace Marvel.Godot;

/// <summary>One column of the separated table: the central stage and shared piles, or one game area.</summary>
/// <param name="GameArea">The engine's game-area id, or null for the central column.</param>
/// <param name="Title">The column's one-line title: its players' names, or the central caption.</param>
/// <param name="Facts">The facts its header states as icons, in display order.</param>
/// <param name="Tooltip">The full explanation of the title and its facts, shown on hover.</param>
/// <param name="ActingStep">The villain-phase step resolving here (1, 2 or 4), or null when this area is not acting.</param>
/// <param name="Villains">Villain object ids placed in this column.</param>
/// <param name="Schemes">Main scheme object ids placed in this column.</param>
/// <param name="Encounter">Side scheme and environment object ids placed in this column.</param>
internal sealed record GameAreaColumn(
    int? GameArea,
    string Title,
    IReadOnlyList<TableFact> Facts,
    string Tooltip,
    int? ActingStep,
    IReadOnlyList<int> Villains,
    IReadOnlyList<int> Schemes,
    IReadOnlyList<int> Encounter)
{
    /// <summary>Whether this is the central column, which holds the shared piles.</summary>
    internal bool Central => GameArea is null;
}
