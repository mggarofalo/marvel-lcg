using Godot;

namespace Marvel.Godot;

/// <summary>Fits measured card detail beside its source within the viewport.</summary>
internal static class CardInspectorPlacement
{
    private const float Margin = 12;
    private const float Gap = 12;

    internal static Rect2 Fit(Vector2 viewport, Rect2 source, Vector2 content,
        IReadOnlyList<Rect2>? occupied = null, IReadOnlyList<Rect2>? actions = null)
    {
        Vector2 available = new(Math.Max(1, viewport.X - Margin * 2),
            Math.Max(1, viewport.Y - Margin * 2));
        Vector2 size = new(Math.Min(content.X, available.X), Math.Min(content.Y, available.Y));
        Rect2[] obstacles = [.. actions ?? [], .. occupied ?? []];
        IEnumerable<Rect2> fitted = Candidates(viewport, source, size, obstacles);
        // Preserve a complete face while keeping its source and current controls exposed.
        return fitted.OrderBy(candidate => Overlap(candidate, source.Grow(Gap / 2)))
            .ThenBy(candidate => actions?.Sum(action => Overlap(candidate, action)) ?? 0)
            .ThenBy(candidate => occupied?.Sum(card => Overlap(candidate, card)) ?? 0)
            .ThenByDescending(candidate => candidate.Size.Y)
            .ThenBy(candidate => candidate.GetCenter().DistanceSquaredTo(source.GetCenter()))
            .First();
    }

    private static IEnumerable<Rect2> Candidates(
        Vector2 viewport, Rect2 source, Vector2 size, IReadOnlyList<Rect2> obstacles)
    {
        Vector2 center = source.GetCenter();
        float[] horizontal = [source.End.X + Gap, source.Position.X - size.X - Gap,
            center.X - size.X / 2, Margin, viewport.X - size.X - Margin,
            .. obstacles.SelectMany(obstacle => new[]
                { obstacle.End.X + Gap, obstacle.Position.X - size.X - Gap })];
        float[] vertical = [center.Y - size.Y / 2, source.Position.Y - size.Y - Gap,
            source.End.Y + Gap, Margin, viewport.Y - size.Y - Margin];
        return horizontal.Distinct().SelectMany(x => vertical.Select(y => new Rect2(new Vector2(
            Math.Clamp(x, Margin, Math.Max(Margin, viewport.X - size.X - Margin)),
            Math.Clamp(y, Margin, Math.Max(Margin, viewport.Y - size.Y - Margin))), size)));
    }

    private static float Overlap(Rect2 left, Rect2 right)
    {
        if (!left.Intersects(right)) return 0;
        Vector2 size = left.Intersection(right).Size;
        return size.X * size.Y;
    }
}
