using Godot;

namespace Marvel.Godot;

/// <summary>Owns the rotation-safe face and bounded local control geometry.</summary>
internal static class SpatialCardFootprint
{
    internal const float SidecarWidth = 132;
    internal const float Gap = 8;

    internal static Vector2 OccupiedSize(Vector2 face) =>
        new(Math.Max(face.X, face.Y), Math.Max(face.X, face.Y) + 40);

    internal static Rect2 Face(Control card) => Bounds(card.GetGlobalTransform(), card.Size);

    internal static Rect2 Bounds(Transform2D pose, Vector2 size)
    {
        Vector2[] corners = [pose * Vector2.Zero, pose * new Vector2(size.X, 0),
            pose * size, pose * new Vector2(0, size.Y)];
        Vector2 start = new(corners.Min(point => point.X), corners.Min(point => point.Y));
        Vector2 end = new(corners.Max(point => point.X), corners.Max(point => point.Y));
        return new Rect2(start, end - start);
    }

    internal static void PlaceSidecar(CardControl card, Control sidecar)
    {
        if (!InteractionControl.IsUsable(card) || !InteractionControl.IsUsable(sidecar)) return;
        Rect2 face = Face(card);
        sidecar.Rotation = -card.Rotation;
        sidecar.Position = ((Control)sidecar.GetParent()).GetGlobalTransform().AffineInverse()
            * new Vector2(face.Position.X, face.End.Y + 4);
        Vector2 scale = card.GetGlobalTransform().Scale.Abs();
        sidecar.Size = new Vector2(face.Size.X / scale.X, 44 / scale.Y);
    }
}
