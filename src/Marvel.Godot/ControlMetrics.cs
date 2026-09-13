namespace Marvel.Godot;

/// <summary>Interactive-control dimensions in logical pixels for one supported scale.</summary>
public sealed record ControlMetrics(
    int MinimumHeight,
    int MinimumPointerTarget,
    int MinimumButtonWidth,
    int FocusRingWidth,
    int CornerRadius);
