using Godot;

namespace Marvel.Godot;

/// <summary>Keeps an ability sheet between chronology and the full hand without moving table objects.</summary>
internal static class AbilityModalLayout
{
    internal static Rect2 Fit(Vector2 viewport, float handTop, float chronologyBottom, float preferredHeight, float preferredWidth = 560)
    {
        float width = Math.Min(preferredWidth, Math.Max(280, viewport.X - 80));
        float top = Math.Max(144, chronologyBottom + 12);
        float bottom = Math.Max(top + 120, Math.Min(viewport.Y - 76, handTop - 12));
        float height = Math.Min(Math.Max(120, preferredHeight), bottom - top);
        return new Rect2(Math.Max(12, viewport.X - width - 56), bottom - height, width, height);
    }
}
