using Godot;

namespace Marvel.Godot;

/// <summary>Finds space for complete detail beside protected objects.</summary>
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
        return free.Where(region => region.Size.X >= content.X && region.Size.Y >= content.Y)
            .OrderBy(region => region.Position.Y).ThenBy(region => region.Position.X)
            .Select(region => new Rect2(region.Position, content)).FirstOrDefault();
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
