using Godot;

namespace Marvel.Godot;

/// <summary>Adds a one-pixel coverage fringe to clockwise B1 polygons in Compatibility rendering.</summary>
internal static class CardEdgeSmoothing
{
    internal static void Add(Polygon2D polygon)
    {
        Vector2[] points = polygon.Polygon;
        Color color = polygon.Color;
        for (int edge = 0; edge < points.Length; edge++)
            polygon.AddChild(new Polygon2D
            {
                Polygon = Fringe(points, edge), VertexColors = Coverage(color),
            });
    }

    internal static void Draw(Control surface, Vector2[] points, Color color)
    {
        for (int edge = 0; edge < points.Length; edge++)
            surface.DrawPolygon(Fringe(points, edge), Coverage(color));
    }

    private static Vector2[] Fringe(Vector2[] points, int edge)
    {
        Vector2 from = points[edge];
        Vector2 to = points[(edge + 1) % points.Length];
        Vector2 direction = (to - from).Normalized();
        Vector2 outward = new(direction.Y, -direction.X);
        return [from, to, to + outward, from + outward];
    }

    private static Color[] Coverage(Color color) =>
        [color, color, new(color, 0), new(color, 0)];
}
