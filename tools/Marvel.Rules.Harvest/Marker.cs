namespace Marvel.Rules.Harvest;

/// <summary>The list marker a line opens with.</summary>
public enum Marker
{
    /// <summary>None — the line is prose, or a wrapped continuation.</summary>
    None,

    /// <summary>A bullet, which the document sets in its heavy weight.</summary>
    Bullet,

    /// <summary>A second-level marker, set in a font that carries no glyph for it.</summary>
    Dash,
}
