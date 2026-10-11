using Godot;

namespace Marvel.Godot;

/// <summary>Stable physical slots; exhaustion and current offers never change their order.</summary>
internal sealed record SourceTableauLayout(float Width, float Height = float.PositiveInfinity, int Count = 0)
{
    internal const float Gap = 6;
    internal const float TileHeight = 84;
    internal int Columns
    {
        get
        {
            int preferred = Math.Max(1, (int)((Width + Gap) / 244));
            bool dense = Count >= 12 && Math.Floor(Height / (TileHeight + Gap)) * preferred < 12;
            return dense ? Math.Max(preferred, (int)((Width + Gap) / 186)) : preferred;
        }
    }
    internal float TileWidth => (Width - (Columns - 1) * Gap) / Columns;
    internal Vector2 Position(int index) => new(index % Columns * (TileWidth + Gap),
        index / Columns * (TileHeight + Gap));
}
