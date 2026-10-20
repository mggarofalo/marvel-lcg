using Godot;

namespace Marvel.Godot;

/// <summary>Fits existing interaction nodes beneath changing chrome without replacing their owners.</summary>
internal sealed class TableCanvasReflow(TableCanvasSurface surface, BoardRenderResult result)
{
    private readonly List<Control> bottom = [];
    private readonly List<Control> middle = [];
    private readonly List<Control> growing = [];
    private readonly float initialHeight = surface.CustomMinimumSize.Y;
    private float middleOffset;

    internal static void AnchorBottom(Control surface, Control control) =>
        ((TableCanvasSurface)surface).Reflow!.bottom.Add(control);

    internal static void Grow(Control surface, Control control) =>
        ((TableCanvasSurface)surface).Reflow!.growing.Add(control);

    internal static void MoveMiddle(Control surface, Control control) =>
        ((TableCanvasSurface)surface).Reflow!.middle.Add(control);

    internal static void MoveAddedMiddle(Control surface, int firstChild)
    {
        foreach (Control child in surface.GetChildren().Skip(firstChild).OfType<Control>())
            MoveMiddle(surface, child);
    }

    internal bool Apply(float height)
    {
        float delta = height - surface.CustomMinimumSize.Y;
        if (delta == 0) return true;
        if (result.PointerLayoutBusy) return false;
        float nextMiddleOffset = MathF.Round((height - initialHeight) / 2);
        float middleShift = nextMiddleOffset - middleOffset;
        middleOffset = nextMiddleOffset;
        // Presentation keeps installed card raster sizes and the villain tableau stable.
        // The player tableau shares the height delta with the bottom-anchored hand.
        surface.CustomMinimumSize = new Vector2(surface.CustomMinimumSize.X, height);
        surface.Size = new Vector2(surface.Size.X, height);
        foreach (Control control in bottom.Where(InteractionControl.IsUsable))
            control.Position += new Vector2(0, delta);
        foreach (Control control in middle.Where(InteractionControl.IsUsable))
        {
            Vector2 shift = new(0, middleShift);
            control.Position += shift;
            result.TranslateRestingPose(control, shift);
        }
        foreach (Control control in growing.Where(InteractionControl.IsUsable))
        {
            Vector2 size = control.Size + new Vector2(0, delta - middleShift);
            if (control.CustomMinimumSize.Y > 0)
                control.CustomMinimumSize += new Vector2(0, delta - middleShift);
            control.Size = size;
        }
        result.TranslateHand(delta);
        return true;
    }
}
