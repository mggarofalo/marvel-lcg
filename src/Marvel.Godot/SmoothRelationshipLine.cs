using Godot;

namespace Marvel.Godot;

/// <summary>Provides explicit edge coverage for table connectors in Compatibility rendering.</summary>
public sealed partial class SmoothRelationshipLine : Line2D
{
    /// <summary>Adds a transparent outer pixel to each immutable connector segment.</summary>
    public override void _Ready()
    {
        for (int index = 1; index < Points.Length; index++)
        {
            Vector2 from = Points[index - 1], to = Points[index];
            Vector2 direction = (to - from).Normalized();
            Vector2 normal = new(-direction.Y, direction.X);
            foreach (int side in new[] { -1, 1 })
            {
                Vector2 inner = normal * (side * Width / 2);
                Vector2 outer = normal * (side * (Width / 2 + 1));
                AddChild(new Polygon2D
                {
                    Polygon = [from + inner, to + inner, to + outer, from + outer],
                    VertexColors = [DefaultColor, DefaultColor, new(DefaultColor, 0), new(DefaultColor, 0)],
                });
            }
        }
    }
}
