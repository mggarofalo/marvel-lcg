using Godot;

namespace Marvel.Godot;

/// <summary>Bounded scroll space when a readable native face cannot fit beside protected objects.</summary>
internal static class CardInspectorFreeSpace
{
    internal static Rect2 Fit(Vector2 viewport, Vector2 content, IReadOnlyList<Rect2> protectedRegions)
    {
        var free = new List<Rect2> { new(new Vector2(12, 12),
            new Vector2(Math.Max(1, viewport.X - 24), Math.Max(1, viewport.Y - 24))) };
        foreach (Rect2 obstacle in protectedRegions)
        {
            free = [.. free.SelectMany(region => Subtract(region, obstacle.Grow(6))).Distinct()];
            free = [.. free.Where(region => !free.Any(other => other != region && other.Encloses(region)))];
        }
        // Width takes priority so a native line remains readable in a vertical scroll surface.
        return free.Select(region => new Rect2(region.Position,
                new Vector2(Math.Min(content.X, region.Size.X), Math.Min(content.Y, region.Size.Y))))
            .Where(region => region.Size.X >= Math.Min(240, content.X) && region.Size.Y >= 120)
            .OrderByDescending(region => region.Size.X >= content.X && region.Size.Y >= 120)
            .ThenByDescending(region => region.Size.X * region.Size.Y).FirstOrDefault();
    }

    private static IEnumerable<Rect2> Subtract(Rect2 region, Rect2 obstacle)
    {
        if (!region.Intersects(obstacle)) { yield return region; yield break; }
        Rect2 cut = region.Intersection(obstacle);
        if (cut.Position.X > region.Position.X)
            yield return new(region.Position, new Vector2(cut.Position.X - region.Position.X, region.Size.Y));
        if (cut.End.X < region.End.X)
            yield return new(new Vector2(cut.End.X, region.Position.Y), new Vector2(region.End.X - cut.End.X, region.Size.Y));
        if (cut.Position.Y > region.Position.Y)
            yield return new(region.Position, new Vector2(region.Size.X, cut.Position.Y - region.Position.Y));
        if (cut.End.Y < region.End.Y)
            yield return new(new Vector2(region.Position.X, cut.End.Y), new Vector2(region.Size.X, region.End.Y - cut.End.Y));
    }
}
