using Godot;

namespace Marvel.Godot;

/// <summary>Reserves the actual laid-out chrome before sizing the fixed desktop table.</summary>
internal static class TableCanvasBounds
{
    internal static float Height(Main main) => Math.Min(AstraTableGeometry.ReferenceHeight,
        Math.Max(820, main.GetViewportRect().Size.Y - main.boardAreas.GetGlobalRect().Position.Y - 4));

    internal static void Observe(Main main, TableCanvasSurface surface)
    {
        void Refresh()
        {
            if (InteractionControl.IsUsable(surface))
                main.boardController.RerenderForViewport(main.GetViewportRect().Size);
        }
        surface.BoundsChanged = Refresh;
        Callable.From(Refresh).CallDeferred();
    }
}
