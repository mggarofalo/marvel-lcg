using Godot;

namespace Marvel.Godot;

/// <summary>Leaves readable gaps where a causal connector passes behind interface copy.</summary>
internal static class ActivityConnectorGeometry
{
    internal static IReadOnlyList<(Vector2 From, Vector2 To)> Segments(
        Vector2 from, Vector2 to, IReadOnlyList<Rect2> exclusions)
    {
        List<(Vector2 From, Vector2 To)> segments = [(from, to)];
        foreach (Rect2 exclusion in exclusions)
            segments = segments.SelectMany(segment => Outside(segment.From, segment.To, exclusion)).ToList();
        return segments;
    }

    private static IEnumerable<(Vector2 From, Vector2 To)> Outside(Vector2 from, Vector2 to, Rect2 box)
    {
        Vector2 delta = to - from;
        float enter = 0, leave = 1;
        if (!Clip(from.X, delta.X, box.Position.X, box.End.X, ref enter, ref leave)
            || !Clip(from.Y, delta.Y, box.Position.Y, box.End.Y, ref enter, ref leave))
        {
            yield return (from, to);
            yield break;
        }
        if (enter > 0) yield return (from, from.Lerp(to, enter));
        if (leave < 1) yield return (from.Lerp(to, leave), to);
    }

    private static bool Clip(float origin, float delta, float minimum, float maximum, ref float enter, ref float leave)
    {
        if (Math.Abs(delta) < .0001f) return origin >= minimum && origin <= maximum;
        float a = (minimum - origin) / delta;
        float b = (maximum - origin) / delta;
        enter = Math.Max(enter, Math.Min(a, b));
        leave = Math.Min(leave, Math.Max(a, b));
        return enter <= leave;
    }
}
