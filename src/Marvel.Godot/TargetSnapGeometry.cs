using Godot;

namespace Marvel.Godot;

/// <summary>Small presentation-only edge tolerance for already authorized destinations.</summary>
internal static class TargetSnapGeometry
{
    internal static float Distance(Rect2 bounds, Vector2 pointer)
    {
        Vector2 closest = pointer.Clamp(bounds.Position, bounds.End);
        return closest.DistanceTo(pointer);
    }

    internal static int? Select(IReadOnlyList<Rect2> bounds, Vector2 pointer, int? previous)
    {
        // Presentation chooses 8px acquisition and 12px release to avoid boundary flicker.
        for (int index = 0; index < bounds.Count; index++)
            if (bounds[index].HasPoint(pointer)) return index;
        if (previous is { } held && held >= 0 && held < bounds.Count
            && Distance(bounds[held], pointer) <= 12) return held;
        return Nearest(bounds, pointer);
    }

    private static int? Nearest(IReadOnlyList<Rect2> bounds, Vector2 pointer)
    {
        int? best = null;
        float distance = 8;
        for (int index = 0; index < bounds.Count; index++)
        {
            float candidate = Distance(bounds[index], pointer);
            if (candidate > distance || (best is not null && candidate == distance)) continue;
            best = index;
            distance = candidate;
        }
        return best;
    }
}
