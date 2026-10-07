using Godot;

namespace Marvel.Godot;

/// <summary>Keeps the scrollable account inside the space left by the table sidebar.</summary>
internal static class TableHistoryViewport
{
    internal static void Configure(Main main, bool payment)
    {
        Bind(main);
        float preferredHeight = payment ? 100 : 300;
        main.eventLog.CustomMinimumSize = new Vector2(0, preferredHeight);
        main.eventLog.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        // Wrapped sidebar copy has its final height after container layout.
        Callable.From(() => Fit(main)).CallDeferred();
    }

    private static void Bind(Main main)
    {
        if (main.eventLog.HasMeta("history_viewport_bound")) return;
        main.eventLog.SetMeta("history_viewport_bound", true);
        main.eventLog.ItemRectChanged += () => Fit(main);
        // Sibling copy can move the log without changing its own local rectangle.
        if (main.eventLog.GetParent() is Container history)
            history.SortChildren += () => Callable.From(() => Fit(main)).CallDeferred();
    }

    private static void Fit(Main main)
    {
        if (!InteractionControl.IsUsable(main) || !main.board.Visible
            || !TableHistoryDrawer.IsExpanded(main)
            || !DesktopTabletop.Uses(main.GetViewportRect().Size))
            return;
        float preferredHeight = CardPaymentWorkspaceLayout.Active(main) ? 100 : 300;
        // The inset is a layout choice, leaving the scrollbar clear of the window edge.
        float available = main.GetViewportRect().End.Y - main.eventLog.GlobalPosition.Y - 16;
        var minimum = new Vector2(0, Math.Clamp(available, 100, preferredHeight));
        if (!main.eventLog.CustomMinimumSize.IsEqualApprox(minimum))
            main.eventLog.CustomMinimumSize = minimum;
    }
}
