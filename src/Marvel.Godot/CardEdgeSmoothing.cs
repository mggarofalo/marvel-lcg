using Godot;

namespace Marvel.Godot;

/// <summary>Adds a one-pixel coverage fringe to clockwise B1 polygons in Compatibility rendering.</summary>
internal static class CardEdgeSmoothing
{
    internal static void DrawFrame(Control surface)
    {
        float radius = CardVisualTokens.FrameRadius;
        Vector2 size = surface.Size;
        Vector2[] centers = [new(size.X - radius, radius), size - Vector2.One * radius,
            new(radius, size.Y - radius), Vector2.One * radius];
        var points = new List<Vector2>();
        for (int corner = 0; corner < 4; corner++)
            for (int step = 0; step <= 8; step++)
                points.Add(centers[corner] + Vector2.FromAngle((corner - 1) * Mathf.Pi / 2 + step * Mathf.Pi / 16) * radius);
        Draw(surface, [.. points], CardFaceStyle.Ink);
    }

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
