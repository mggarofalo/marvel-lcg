using Godot;

namespace Marvel.Godot;

/// <summary>Keeps the scrollable account inside the space left by the table sidebar.</summary>
internal static class TableHistoryViewport
{
    internal static void Configure(Main main, bool payment)
    {
        Bind(main);
        float preferredHeight = payment ? 180 : 440;
        SetHeight(Viewport(main), preferredHeight);
        Viewport(main).SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        // Wrapped sidebar copy has its final height after container layout.
        Callable.From(() => Fit(main)).CallDeferred();
    }

    private static Control Viewport(Main main) =>
        main.eventLog.GetParent().GetNodeOrNull<Control>("HistoryOccurrences") ?? main.eventLog;

    private static void Bind(Main main)
    {
        if (Viewport(main).HasMeta("history_viewport_bound")) return;
        Viewport(main).SetMeta("history_viewport_bound", true);
        Viewport(main).ItemRectChanged += () => Fit(main);
        // Sibling copy can move the log without changing its own local rectangle.
        if (Viewport(main).GetParent() is Container history)
            history.SortChildren += () => Callable.From(() => Fit(main)).CallDeferred();
    }

    internal static void Fit(Main main)
    {
        if (!InteractionControl.IsUsable(main) || !main.board.Visible
            || !TableHistoryDrawer.IsExpanded(main)
            || !DesktopTabletop.Uses(main.GetViewportRect().Size))
            return;
        float minimumHeight = CardPaymentWorkspaceLayout.Active(main) ? 100 : 180;
        // The inset is a layout choice, leaving the scrollbar clear of the window edge.
        float bottom = main.promptPanel.TopLevel ? HistoryOverlayLayout.Bottom(main) : main.GetViewportRect().End.Y;
        float available = bottom - Viewport(main).GlobalPosition.Y - 16;
        SetHeight(Viewport(main), Math.Max(minimumHeight, available));
    }

    private static void SetHeight(Control viewport, float height)
    {
        if (viewport is HistoryOccurrenceList list) list.SetViewportHeight(height);
        else if (!viewport.CustomMinimumSize.IsEqualApprox(new Vector2(0, height)))
            viewport.CustomMinimumSize = new Vector2(0, height);
    }
}
