using Godot;

namespace Marvel.Godot;

/// <summary>An explicit reading mode when no independent rectangle remains beside the live decision.</summary>
internal static class CardInspectorOverlayPlacement
{
    internal static Rect2 Fit(Vector2 viewport, Rect2 source, Vector2 content, bool pinned)
    {
        Vector2 size = pinned ? content : new Vector2(Math.Max(220, source.Size.X), 48);
        size = new Vector2(Math.Min(size.X, viewport.X - 24), Math.Min(size.Y, viewport.Y - 24));
        Vector2 position = pinned ? (viewport - size) / 2 : source.Position;
        return new(new Vector2(Math.Clamp(position.X, 12, Math.Max(12, viewport.X - size.X - 12)),
            Math.Clamp(position.Y, 12, Math.Max(12, viewport.Y - size.Y - 12))), size);
    }
}
