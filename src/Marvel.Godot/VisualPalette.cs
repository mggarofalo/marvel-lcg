namespace Marvel.Godot;

/// <summary>The semantic colors shared by every client surface.</summary>
public sealed record VisualPalette(
    VisualColor Canvas,
    VisualColor Surface,
    VisualColor RaisedSurface,
    VisualColor Text,
    VisualColor MutedText,
    VisualColor Accent,
    VisualColor OnAccent,
    VisualColor Legal,
    VisualColor Selected,
    VisualColor Danger,
    VisualColor Unavailable,
    VisualColor Outline,
    VisualColor Focus);
