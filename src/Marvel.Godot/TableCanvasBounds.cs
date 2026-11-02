using Godot;

namespace Marvel.Godot;

/// <summary>Reserves the actual laid-out chrome before sizing the fixed desktop table.</summary>
internal static class TableCanvasBounds
{
    // Godot containers occupy whole pixels. Fractional ancestor placement must not
    // reconstruct a decision whose physical canvas size has not changed. The table takes the
    // window's whole height below the chrome; a large window grows the table rather than
    // leaving empty space below it.
    // It ends at the page's anchored bottom less the shell's own bottom margin, so a full-height table
    // never makes the page scroll. The page's anchors, not its current rectangle, give that bottom: a page
    // that does not scroll grows to its content, and the table must not grow with it.
    internal static float Height(Main main)
    {
        float bottom = main.GetViewportRect().Size.Y + main.pageScroll.OffsetBottom
            - main.GetNode<PanelContainer>("Margin/Shell").GetThemeStylebox("panel").GetMargin(Side.Bottom);
        return Fit(bottom - main.boardAreas.GetGlobalRect().Position.Y - 4);
    }

    /// <summary>
    /// The table's height in the room below the chrome: the reference height, or less down to 820, until the
    /// room is a whole card-growth step taller; from there the table takes it all. Below that step a few
    /// pixels of chrome moving never redraws the table.
    /// </summary>
    internal static float Fit(float room)
    {
        float available = Math.Max(820, MathF.Floor(room));
        return available >= AstraTableGeometry.ReferenceHeight * 1.1f
            ? available : Math.Min(AstraTableGeometry.ReferenceHeight, available);
    }

    /// <summary>
    /// Whether a table rendered at one height can be stretched to another in place: its cards and far side
    /// grow in steps with its height, so it can while both heights give the same step, and is drawn again
    /// when the step changes.
    /// </summary>
    internal static bool Reflowable(float rendered, float height) =>
        SpatialCardMetrics.TableScale(InterfaceScale.Percent100, Math.Max(rendered, AstraTableGeometry.ReferenceHeight))
        == SpatialCardMetrics.TableScale(InterfaceScale.Percent100, Math.Max(height, AstraTableGeometry.ReferenceHeight));

    internal static void Observe(Main main, TableCanvasSurface surface)
    {
        bool Refresh()
        {
            if (!InteractionControl.IsUsable(main) || !InteractionControl.IsUsable(surface)
                || main.CurrentGame?.World is null) return true;
            float height = Height(main);
            if (!Reflowable(surface.CustomMinimumSize.Y, height))
            {
                Callable.From(() =>
                {
                    if (!InteractionControl.IsUsable(main)) return;
                    main.boardController.RerenderForViewport(main.GetViewportRect().Size);
                    Callable.From(main.eventController.FitTimeline).CallDeferred();
                }).CallDeferred();
                return true;
            }
            if (surface.Reflow?.Apply(height) == false) return false;
            main.boardController.RecordTableHeight(height);
            return true;
        }
        surface.BoundsChanged = Refresh;
    }
}
