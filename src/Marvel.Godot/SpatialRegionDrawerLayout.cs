using Godot;

namespace Marvel.Godot;

/// <summary>Owns the minimum hit area and bounded width of a table region's collection control.</summary>
internal static class SpatialRegionDrawerLayout
{
    internal const float MinimumWidth = 80;

    internal static Rect2 Bounds(Rect2 region) => new(region.Position,
        new Vector2(Math.Clamp(region.Size.X, MinimumWidth, 220), 44));
}
