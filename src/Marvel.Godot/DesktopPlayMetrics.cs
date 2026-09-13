namespace Marvel.Godot;

/// <summary>Desktop workbench dimensions derived from the current viewport and scale.</summary>
public sealed record DesktopPlayMetrics(
    int DecisionDockHeight,
    int BoardAreaWidth);
