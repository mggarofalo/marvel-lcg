namespace Marvel.Godot;

/// <summary>The two disclosure levels supported by a procedural card.</summary>
public enum CardDisplaySize
{
    Full,
    Board,
    Hand,

    /// <summary>A readable opening-hand card that fits all six mulligan choices on the desktop table.</summary>
    Mulligan,
}
