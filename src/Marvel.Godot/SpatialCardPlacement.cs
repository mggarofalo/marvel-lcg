using Godot;

namespace Marvel.Godot;

/// <summary>One presentation-only transform in the private hand fan.</summary>
internal sealed record SpatialCardPlacement(
    Vector2 Position,
    float Rotation,
    int ZIndex,
    bool Overlaps);
