using Godot;

namespace Marvel.Godot;

/// <summary>Reveals bounded pinned detail at its final raster size.</summary>
internal sealed partial class CardInspectorTransition : Node
{
    private Control? frame;
    private Func<bool>? motionEnabled;
    private double elapsed;
    private const double Duration = 0.12;

    internal void Expand(Control detail, Control? source, bool animate, Func<bool> enabled)
    {
        Settle();
        frame = detail;
        motionEnabled = enabled;
        if (!animate) return;
        // Keep glyphs at their final raster size throughout the reveal.
        detail.Modulate = new Color(1, 1, 1, 0);
        elapsed = 0;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (!InteractionControl.IsUsable(frame) || !frame!.IsVisibleInTree()
            || motionEnabled?.Invoke() != true)
        {
            Settle();
            return;
        }
        elapsed += delta;
        float fraction = (float)Math.Clamp(elapsed / Duration, 0, 1);
        float eased = 1 - (1 - fraction) * (1 - fraction);
        frame.Modulate = new Color(1, 1, 1, eased);
        if (fraction >= 1) Settle();
    }

    private void Settle()
    {
        if (InteractionControl.IsUsable(frame)) frame!.Modulate = Colors.White;
        SetProcess(false);
    }
}
