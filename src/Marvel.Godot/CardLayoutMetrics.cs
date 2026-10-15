namespace Marvel.Godot;

/// <summary>Deterministic card geometry and text disclosure for one display size.</summary>
public sealed record CardLayoutMetrics(
    int Width,
    int MinimumHeight,
    bool ShowSubtitle,
    bool ShowTraits,
    bool ShowPrintedStats)
{
    /// <summary>Native compact typography can increase independently of the physical card footprint.</summary>
    public InterfaceScale ReadingScale { get; init; } = InterfaceScale.Standard;
}
