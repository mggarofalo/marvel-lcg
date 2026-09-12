namespace Marvel.Godot;

/// <summary>A semantic control treatment that a renderer can map to native theme values.</summary>
public sealed record InteractiveStyle(
    string ThemeVariation,
    VisualColor Foreground,
    VisualColor Background,
    VisualColor Border,
    NonColorCue Cues,
    int BorderWidth,
    int FocusRingWidth,
    int VerticalOffset,
    bool Enabled);
