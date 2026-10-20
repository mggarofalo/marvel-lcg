using Godot;

namespace Marvel.Godot;

/// <summary>Reserves the actual laid-out chrome before sizing the fixed desktop table.</summary>
internal static class TableCanvasBounds
{
    // Godot containers occupy whole pixels. Fractional ancestor placement must not
    // reconstruct a decision whose physical canvas size has not changed.
    internal static float Height(Main main) => Math.Min(AstraTableGeometry.ReferenceHeight,
        Math.Max(820, MathF.Ceiling(main.GetViewportRect().Size.Y - main.boardAreas.GetGlobalRect().Position.Y - 4)));

    internal static void Observe(Main main, TableCanvasSurface surface)
    {
        bool Refresh()
        {
            if (!InteractionControl.IsUsable(main) || !InteractionControl.IsUsable(surface)
                || main.CurrentGame?.World is null) return true;
            float height = Height(main);
            if (surface.Reflow?.Apply(height) == false) return false;
            main.boardController.RecordTableHeight(height);
            return true;
        }
        surface.BoundsChanged = Refresh;
    }
}
