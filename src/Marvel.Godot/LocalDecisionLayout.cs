using Godot;

namespace Marvel.Godot;

/// <summary>Fits a local editor beside its source while retaining access to the hand.</summary>
internal static class LocalDecisionLayout
{
    internal static Rect2 Fit(Vector2 viewport, Rect2 source, float handTop, float wantedHeight)
    {
        float width = Math.Min(580, viewport.X - 32);
        float bottom = Math.Clamp(handTop - 12, 300, viewport.Y - 16);
        float height = Math.Clamp(wantedHeight, 200, Math.Max(200, bottom - 80));
        float right = source.End.X + 12;
        float left = source.Position.X - width - 12;
        float x = right + width <= viewport.X - 16 ? right : left >= 16 ? left : viewport.X - width - 16;
        return new(new Vector2(Math.Clamp(x, 16, viewport.X - width - 16),
            Math.Clamp(source.Position.Y, 72, Math.Max(72, bottom - height))), new Vector2(width, height));
    }
}
