using Godot;

namespace Marvel.Godot;

/// <summary>Consumes timeline navigation without sending table gestures or game decisions.</summary>
internal sealed class TimelineRailInput(Control rail, TimelineViewport viewport, Action refresh, Action<int> preview,
    Action requestDetail)
{
    private bool dragging;
    private bool moved;
    private Vector2 start;

    internal void Handle(InputEvent input)
    {
        if (input is InputEventMouseButton button) HandleMouseButton(button);
        else if (input is InputEventMouseMotion motion) MouseMotion(motion);
        else if (input is InputEventKey { Pressed: true } key) Key(key);
    }

    private void HandleMouseButton(InputEventMouseButton button)
    {
        double anchor = button.Position.X / Math.Max(1, rail.Size.X);
        if (button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
        {
            if (!button.Pressed) return;
            double direction = button.ButtonIndex == MouseButton.WheelUp ? -1 : 1;
            if (button.ShiftPressed) viewport.Pan(direction * viewport.Span / 5);
            else viewport.Zoom(direction < 0 ? .7 : 1 / .7, anchor);
        }
        else if (button.ButtonIndex == MouseButton.Left)
        {
            dragging = button.Pressed;
            if (dragging) { start = button.Position; moved = false; rail.GrabFocus(); }
            else if (!moved) { viewport.Select(viewport.At(anchor)); requestDetail(); }
        }
        else return;
        rail.AcceptEvent();
        refresh();
    }

    private void MouseMotion(InputEventMouseMotion motion)
    {
        if (dragging)
        {
            moved |= motion.Position.DistanceTo(start) > 4;
            if (moved) viewport.Pan(-motion.Relative.X / Math.Max(1, rail.Size.X) * viewport.Span);
            refresh();
        }
        else preview(viewport.At(motion.Position.X / Math.Max(1, rail.Size.X)));
        rail.AcceptEvent();
    }

    private static global::Godot.Key CanonicalKey(global::Godot.Key key) =>
        key == global::Godot.Key.Equal ? global::Godot.Key.Plus : key;

    private void Key(InputEventKey key)
    {
        if (key.Keycode == global::Godot.Key.Escape) { preview(-1); rail.AcceptEvent(); return; }
        int previous = viewport.Selected;
        switch (CanonicalKey(key.Keycode))
        {
            case global::Godot.Key.Left: viewport.Select(viewport.Selected - 1); break;
            case global::Godot.Key.Right: viewport.Select(viewport.Selected + 1); break;
            case global::Godot.Key.Home: viewport.Select(0); break;
            case global::Godot.Key.End: viewport.Latest(); break;
            case global::Godot.Key.Plus: viewport.Zoom(.7, .5); break;
            case global::Godot.Key.Minus: viewport.Zoom(1 / .7, .5); break;
            case global::Godot.Key.Pageup: viewport.Pan(-viewport.Span / 2); break;
            case global::Godot.Key.Pagedown: viewport.Pan(viewport.Span / 2); break;
            default: return;
        }
        rail.AcceptEvent();
        refresh();
        NotifySelection(previous);
        preview(viewport.Selected);
    }
    private void NotifySelection(int previous)
    {
        if (viewport.Selected != previous) requestDetail();
    }

}
