using Godot;

namespace Marvel.Godot;

/// <summary>Opens read-only history above the table without moving its physical anchors.</summary>
internal static class HistoryOverlayLayout
{
    internal static void Configure(Main main, bool desktop, bool payment)
    {
        bool overlay = desktop && TableHistoryDrawer.IsExpanded(main) && !payment;
        main.promptPanel.TopLevel = overlay;
        main.promptPanel.ZIndex = overlay ? 80 : 0;
        if (!overlay) return;
        Callable.From(() =>
        {
            if (!InteractionControl.IsUsable(main) || !main.promptPanel.TopLevel) return;
            Vector2 viewport = main.GetViewportRect().Size;
            float width = TableHistoryDrawer.ExpandedWidth(main);
            float bottom = Bottom(main);
            float top = 88;
            if (main.FindChild("EventTimeline", true, false) is Control timeline && timeline.IsVisibleInTree())
                top += timeline.Size.Y + 8;
            main.promptPanel.GlobalPosition = new Vector2(viewport.X - width - 16, top);
            TableHistoryViewport.Fit(main);
            main.promptPanel.ResetSize();
            main.promptPanel.Size = new Vector2(width, Math.Max(0, bottom - top));
        }).CallDeferred();
    }

    internal static float Bottom(Main main) => main.boardRender?.VisibleCardControls()
        .Where(card => card.HasMeta("spatial_hand_index") && card.IsVisibleInTree())
        .Select(card => SpatialCardFootprint.Face(card).Position.Y - 12)
        .DefaultIfEmpty(main.GetViewportRect().Size.Y - 16).Min() ?? main.GetViewportRect().Size.Y - 16;
}
