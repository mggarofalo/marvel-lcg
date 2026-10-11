using Godot;

namespace Marvel.Godot;

/// <summary>Expands bounded pinned detail without moving its physical table source.</summary>
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
        Vector2 origin = InteractionControl.IsUsable(source)
            ? source!.GetGlobalRect().GetCenter() - detail.GlobalPosition : detail.Size / 2;
        detail.PivotOffset = origin.Clamp(Vector2.Zero, detail.Size);
        detail.Scale = new Vector2(0.96f, 0.96f);
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
        frame.Scale = Vector2.One * (0.96f + 0.04f * eased);
        if (fraction >= 1) Settle();
    }

    private void Settle()
    {
        if (InteractionControl.IsUsable(frame)) frame!.Scale = Vector2.One;
        SetProcess(false);
    }
}
