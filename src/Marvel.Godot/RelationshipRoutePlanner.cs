using Godot;

namespace Marvel.Godot;

/// <summary>Finds a measured orthogonal fallback path around visible card rectangles.</summary>
internal static class RelationshipRoutePlanner
{
    internal static Vector2[]? Route(Rect2 source, Rect2 target, IReadOnlyList<Rect2> obstacles)
    {
        Vector2 start = source.GetCenter();
        Vector2 finish = target.GetCenter();
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
        return Clear(vertical, obstacles) ? vertical : null;
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
