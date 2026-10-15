using Godot;

namespace Marvel.Godot;

/// <summary>Marks a destination outside its existing silhouette without changing layout.</summary>
internal static class DropSurfaceCue
{
    internal static void Set(Control surface, bool active)
    {
        if (!surface.HasMeta("drop_surface_cue_bound"))
        {
            surface.SetMeta("drop_surface_cue_bound", true);
            surface.Draw += () =>
            {
                if (surface.GetMeta("spatial_drop_active", false).AsBool())
                    surface.DrawRect(new Rect2(Vector2.Zero, surface.Size).Grow(4),
                        ClientTheme.ToGodot(VisualSystem.Palette.Accent), false, 2);
            };
        }
        surface.SetMeta("spatial_drop_active", active);
        surface.QueueRedraw();
    }
}
