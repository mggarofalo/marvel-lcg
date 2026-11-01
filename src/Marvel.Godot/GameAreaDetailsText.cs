using Godot;

namespace Marvel.Godot;

/// <summary>A column's explanation sentences in a scrolling viewport, measured before the column is arranged.</summary>
internal sealed class GameAreaDetailsText(ScrollContainer viewport, Label text)
{
    /// <summary>The wrapped text's full height at the column width.</summary>
    internal float Measured { get; } = text.GetCombinedMinimumSize().Y;

    /// <summary>The distance from one wrapped line to the next.</summary>
    internal float Line { get; } = text.GetLineHeight() + text.GetThemeConstant("line_spacing");

    /// <summary>Places the viewport; text beyond its height scrolls with a visible scroll bar.</summary>
    internal void Place(Vector2 position, float height)
    {
        viewport.Position = position;
        viewport.Size = new Vector2(viewport.Size.X, height);
        viewport.Visible = height > 0;
    }
}
