using Godot;

namespace Marvel.Godot;

/// <summary>Owns the cursor and bounded local explanation for one captured pointer.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
    Justification = "The source scene owns the feedback controls; completion detaches all window signals.")]
internal sealed class DragPointerFeedback
{
    private readonly Dictionary<Control, Control.CursorShape> cursors = [];
    private Control? source;
    private Label? cue;
    private Window? window;
    private Action? cancelled;
    private Vector2 pointer;

    internal void Begin(Control card, Action cancel)
    {
        End();
        ClearCue();
        source = card;
        cancelled = cancel;
        window = card.GetWindow();
        window.FocusExited += Cancel;
        card.TreeExiting += End;
        foreach (Control control in Controls(card)) cursors.Add(control, control.MouseDefaultCursorShape);
        SetCursor(Control.CursorShape.Drag);
    }

    internal void Preview(Vector2 position, bool accepted, string rejected = "No offered action here")
    {
        pointer = position;
        SetCursor(accepted ? Control.CursorShape.CanDrop : Control.CursorShape.Forbidden);
        if (accepted) ClearCue();
        else ShowCue(rejected);
    }

    internal void Finish(string message)
    {
        if (message.Length > 0) ShowCue(message);
        else ClearCue();
        End();
        if (!InteractionControl.IsUsable(cue)) return;
        Label retained = cue!;
        Tween lifetime = retained.CreateTween();
        lifetime.TweenInterval(0.9);
        lifetime.TweenCallback(Callable.From(() =>
        {
            if (InteractionControl.IsUsable(retained)) { retained.Hide(); retained.QueueFree(); }
            if (cue == retained) cue = null;
        }));
    }

    private void ShowCue(string message)
    {
        if (!InteractionControl.IsUsable(source)) return;
        if (!InteractionControl.IsUsable(cue))
        {
            cue = CardSourceStrip.Label(message, "DragLocalFeedback", 14, CardTypography.Bold);
            cue.AutowrapMode = TextServer.AutowrapMode.Off;
            cue.TopLevel = true;
            cue.ZIndex = 250;
            cue.MouseFilter = Control.MouseFilterEnum.Ignore;
            cue.AccessibilityLive = AccessibilityServer.AccessibilityLiveMode.Polite;
            cue.AddThemeColorOverride("font_color", ClientTheme.ToGodot(VisualSystem.Palette.Text));
            using var paper = new StyleBoxFlat { BgColor = new Color("142631"),
                ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 5, ContentMarginBottom = 5,
                BorderColor = new Color("36505D"), BorderWidthBottom = 1, BorderWidthTop = 1,
                BorderWidthLeft = 1, BorderWidthRight = 1, CornerRadiusTopLeft = 6,
                CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6 };
            cue.AddThemeStyleboxOverride("normal", paper);
            source!.AddChild(cue);
        }
        cue!.Text = message;
        cue.AccessibilityName = message;
        cue.Size = cue.GetCombinedMinimumSize();
        Vector2 extent = source!.GetViewport().GetVisibleRect().Size;
        Vector2 desired = pointer + new Vector2(18, 18);
        cue.GlobalPosition = new Vector2(Math.Clamp(desired.X, 8, Math.Max(8, extent.X - cue.Size.X - 8)),
            Math.Clamp(desired.Y, 8, Math.Max(8, extent.Y - cue.Size.Y - 8)));
    }

    private void SetCursor(Control.CursorShape shape)
    {
        Input.SetDefaultCursorShape((Input.CursorShape)shape);
        foreach (Control control in cursors.Keys.Where(InteractionControl.IsUsable))
            control.MouseDefaultCursorShape = shape;
    }

    private void End()
    {
        foreach ((Control control, Control.CursorShape shape) in cursors)
            if (GodotObject.IsInstanceValid(control)) control.MouseDefaultCursorShape = shape;
        cursors.Clear();
        Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
        if (GodotObject.IsInstanceValid(window)) window!.FocusExited -= Cancel;
        if (GodotObject.IsInstanceValid(source)) source!.TreeExiting -= End;
        source = null;
        window = null;
        cancelled = null;
    }

    private void Cancel() => cancelled?.Invoke();
    private void ClearCue()
    {
        if (InteractionControl.IsUsable(cue)) { cue!.Hide(); cue.QueueFree(); }
        cue = null;
    }

    private static IEnumerable<Control> Controls(Node node)
    {
        if (node is Control control) yield return control;
        foreach (Node child in node.GetChildren())
            foreach (Control descendant in Controls(child)) yield return descendant;
    }
}
