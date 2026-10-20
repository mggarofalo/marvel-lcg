using Godot;

namespace Marvel.Godot;

/// <summary>Retains authorized resolution or invalidation feedback beside the initiating card.</summary>
internal sealed class LocalDecisionReceipt(DecisionPanel panel) : IDisposable
{
    private CanvasLayer? layer;
    private int generation;

    internal void Present(Rect2 anchor, string origin, bool submitted)
    {
        Dispose();
        int expected = generation;
        Callable.From(() => Show(anchor, origin, submitted, expected)).CallDeferred();
    }

    private void Show(Rect2 anchor, string origin, bool submitted, int expected)
    {
        if (expected != generation || !panel.IsInsideTree()) return;
        Main main = ClientSceneHost.MainFor(panel);
        layer = new CanvasLayer { Name = "LocalDecisionReceiptLayer", Layer = 89 };
        main.AddChild(layer);
        var frame = new PanelContainer { Name = "LocalDecisionReceipt", Theme = panel.Theme,
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel };
        layer.AddChild(frame);
        var content = new VBoxContainer();
        frame.AddChild(content);
        content.AddChild(DecisionPanel.Text(origin, GodotThemeVariations.Body, wrap: true));
        string result = submitted ? main.lastResultSummary.Text
            : "The table changed. Review the current choices before acting again.";
        if (string.IsNullOrWhiteSpace(result)) result = "Choice submitted. Review the current table and next decision.";
        content.AddChild(DecisionPanel.Text(result, GodotThemeVariations.Caption, wrap: true));
        var close = new Button { Name = "LocalReceiptDismiss", Text = "Dismiss" };
        close.Pressed += Dispose;
        content.AddChild(close);
        frame.MinimumSizeChanged += () => Callable.From(() => Fit(main, frame, anchor)).CallDeferred();
        Fit(main, frame, anchor);
        Callable.From(() => Fit(main, frame, anchor)).CallDeferred();
    }

    private static void Fit(Main main, PanelContainer frame, Rect2 anchor)
    {
        if (!InteractionControl.IsUsable(frame)) return;
        Control? hand = main.FindChild("HandDropArea", true, false) as Control;
        Rect2 bounds = LocalDecisionLayout.Fit(main.GetViewportRect().Size, anchor,
            hand?.GetGlobalRect().Position.Y ?? main.GetViewportRect().Size.Y - 250, 200);
        frame.Position = bounds.Position;
        frame.Size = bounds.Size;
    }

    public void Dispose()
    {
        generation++;
        if (GodotObject.IsInstanceValid(layer)) { layer!.Visible = false; layer.QueueFree(); }
        layer = null;
    }
}
