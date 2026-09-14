using Godot;

namespace Marvel.Godot;

/// <summary>Finds a measured orthogonal fallback path around visible card rectangles.</summary>
internal static class RelationshipRoutePlanner
{
    internal static Vector2[]? Route(Rect2 source, Rect2 target, IReadOnlyList<Rect2> obstacles)
    {
        (Vector2 start, Vector2 finish) = CardEdges(source, target);
        if (Clear([start, finish], obstacles))
        {
            return [start, finish];
        }

        Vector2[] horizontal = [start, new Vector2(finish.X, start.Y), finish];
        if (Clear(horizontal, obstacles))
        {
            return horizontal;
        }

        Vector2[] vertical = [start, new Vector2(start.X, finish.Y), finish];
        if (Clear(vertical, obstacles))
        {
            return vertical;
        }

        foreach (Vector2[] detour in Detours(start, finish, obstacles))
        {
            if (Clear(detour, obstacles))
            {
                return detour;
            }
        }

        return null;
    }

    private static (Vector2 Start, Vector2 Finish) CardEdges(Rect2 source, Rect2 target)
    {
        Vector2 sourceCenter = source.GetCenter();
        Vector2 targetCenter = target.GetCenter();
        if (Mathf.Abs(targetCenter.X - sourceCenter.X) >= Mathf.Abs(targetCenter.Y - sourceCenter.Y))
        {
            return targetCenter.X >= sourceCenter.X
                ? (new Vector2(source.End.X, sourceCenter.Y), new Vector2(target.Position.X, targetCenter.Y))
                : (new Vector2(source.Position.X, sourceCenter.Y), new Vector2(target.End.X, targetCenter.Y));
        }

        return targetCenter.Y >= sourceCenter.Y
            ? (new Vector2(sourceCenter.X, source.End.Y), new Vector2(targetCenter.X, target.Position.Y))
            : (new Vector2(sourceCenter.X, source.Position.Y), new Vector2(targetCenter.X, target.End.Y));
    }

    private static IEnumerable<Vector2[]> Detours(
        Vector2 start,
        Vector2 finish,
        IReadOnlyList<Rect2> obstacles)
    {
        const float clearance = 8;
        float top = obstacles.Min(rect => rect.Position.Y) - clearance;
        float bottom = obstacles.Max(rect => rect.End.Y) + clearance;
        float left = obstacles.Min(rect => rect.Position.X) - clearance;
        float right = obstacles.Max(rect => rect.End.X) + clearance;
        yield return [start, new Vector2(start.X, top), new Vector2(finish.X, top), finish];
        yield return [start, new Vector2(start.X, bottom), new Vector2(finish.X, bottom), finish];
        yield return [start, new Vector2(left, start.Y), new Vector2(left, finish.Y), finish];
        yield return [start, new Vector2(right, start.Y), new Vector2(right, finish.Y), finish];
    }

    private static bool Clear(Vector2[] path, IReadOnlyList<Rect2> obstacles)
    {
        for (int index = 1; index < path.Length; index++)
        {
            Vector2 delta = path[index] - path[index - 1];
            int samples = Math.Max(1, Mathf.CeilToInt(delta.Length() / 8));
            for (int sample = 1; sample < samples; sample++)
            {
                Vector2 point = path[index - 1] + delta * sample / samples;
                if (obstacles.Any(obstacle => obstacle.Grow(2).HasPoint(point)))
                {
                    return false;
                }
            }
        }
        return true;
    }
}
