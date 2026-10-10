using Godot;

namespace Marvel.Godot;

/// <summary>Stable physical slots; exhaustion and current offers never change their order.</summary>
internal sealed record SourceTableauLayout(float Width)
{
    internal const float Gap = 6;
    internal const float TileHeight = 144;
    internal int Columns => Math.Max(1, (int)((Width + Gap) / 156));
    internal float TileWidth => (Width - (Columns - 1) * Gap) / Columns;
    internal Vector2 Position(int index) => new(index % Columns * (TileWidth + Gap),
        index / Columns * (TileHeight + Gap));
}
