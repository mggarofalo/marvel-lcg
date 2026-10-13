using Godot;

namespace Marvel.Godot;

/// <summary>Stable physical slots; exhaustion and current offers never change their order.</summary>
internal sealed record SourceTableauLayout(float Width, float Height = float.PositiveInfinity, int Count = 0)
{
    internal const float Gap = 6;
    internal const float TileHeight = 60;
    internal int Columns => Math.Clamp((int)((Width + Gap) / 150), 1, 4);
    internal int Capacity => Columns * Math.Max(1, Math.Max(
        (int)Math.Ceiling((double)Count / Columns),
        float.IsFinite(Height) ? (int)Math.Floor((Height + Gap) / (TileHeight + Gap)) : 0));
    internal float ContentHeight(int slots) =>
        Math.Max(0, (int)Math.Ceiling((double)slots / Columns) * (TileHeight + Gap) - Gap);
    internal float TileWidth => (Width - (Columns - 1) * Gap) / Columns;
    internal Vector2 Position(int index) => new(index % Columns * (TileWidth + Gap),
        index / Columns * (TileHeight + Gap));
}
