using Godot;

namespace Marvel.Godot;

/// <summary>The seat rail's overflow cue, counted from the tiles' laid-out edges and kept current as the rail scrolls.</summary>
internal static class SeatRailOverflow
{
    /// <summary>Adds the cue at the rail's foot and recounts whenever the rail scrolls or its tiles resize.</summary>
    internal static Label Add(Control surface, ScrollContainer viewport, Control strip, InterfaceScale scale)
    {
        Label cue = OverflowCue.Create("SeatOverflow", 0, TableWording.SeatOverflow, scale);
        cue.Text = OverflowCue.Text(1); // sized for one line of the cue; the first count replaces it
        cue.Size = new Vector2(viewport.Size.X - 16, cue.GetCombinedMinimumSize().Y);
        cue.Position = viewport.Position + new Vector2(0, viewport.Size.Y - cue.Size.Y);
        cue.ZIndex = 25;
        surface.AddChild(cue);
        void Recount()
        {
            if (!GodotObject.IsInstanceValid(cue) || !viewport.IsInsideTree()) return;
            Rect2 view = viewport.GetGlobalRect();
            OverflowCue.Show(cue, OverflowCue.Hidden(Tiles(strip).Select(tile => tile.GetGlobalRect())
                .Select(rect => (rect.Position.Y, rect.End.Y)), view.Position.Y, view.End.Y));
        }
        viewport.GetVScrollBar().ValueChanged += _ => Recount();
        strip.Resized += Recount;
        Callable.From(Recount).CallDeferred();
        return cue;
    }

    private static IEnumerable<Control> Tiles(Control strip) =>
        strip.FindChildren("SeatSwitch*", "Button", true, false).OfType<Control>().Where(tile => tile.Visible);
}
