namespace Marvel.Godot;

/// <summary>Deterministic card geometry and text disclosure for one display size.</summary>
public sealed record CardLayoutMetrics(
    int Width,
    int MinimumHeight,
    bool ShowSubtitle,
    bool ShowTraits,
    bool ShowPrintedStats);
