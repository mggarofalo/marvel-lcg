namespace Marvel.View;

/// <summary>One game-area grouping and what it holds.</summary>
/// <param name="Id">The engine's game-area id, unique within the game.</param>
/// <param name="PlayAreas">The play areas in it: seats, and <c>-1</c> for the villain's.</param>
public sealed record GameAreaDescriptor(int Id, IReadOnlyList<int> PlayAreas)
{
    /// <summary>The villains in play in this area, by object id.</summary>
    public IReadOnlyList<int> Villains { get; init; } = [];

    /// <summary>The main schemes in play in this area, by object id.</summary>
    public IReadOnlyList<int> MainSchemes { get; init; } = [];

    /// <summary>Whether this area holds the villain's play area.</summary>
    public bool Central { get; init; }

    /// <summary>Whether this area has no players but holds an eliminated player's components until they depart.</summary>
    public bool Retained { get; init; }

    /// <summary>Eliminated seats whose retained components this area still holds.</summary>
    public IReadOnlyList<int> FormerSeats { get; init; } = [];

    /// <summary>The engine-authored display name, naming its players or central role and its villains.</summary>
    public string Label { get; init; } = "";
}
