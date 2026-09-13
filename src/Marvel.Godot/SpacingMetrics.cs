namespace Marvel.Godot;

/// <summary>Layout intervals in logical pixels for one supported interface scale.</summary>
public sealed record SpacingMetrics(
    int ExtraSmall,
    int Small,
    int Medium,
    int Large,
    int ExtraLarge,
    int Section);
