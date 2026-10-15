using Godot;

namespace Marvel.Godot;

/// <summary>Records and restores the table pose beneath temporary pointer presentation.</summary>
internal sealed class CardPointerRestingPoses
{
    private readonly Dictionary<Control, (Vector2 Position, float Rotation, int Z)> poses = [];

    internal void Store(Control control)
    {
        poses[control] = (control.Position, control.Rotation, control.ZIndex);
        SpatialCardPose.Store(control);
    }

    internal bool TryGetValue(Control control, out (Vector2 Position, float Rotation, int Z) pose) =>
        poses.TryGetValue(control, out pose);

    internal void Restore(Control control)
    {
        if (!poses.TryGetValue(control, out var pose) || !InteractionControl.IsUsable(control)) return;
        control.Position = pose.Position;
        control.Rotation = pose.Rotation;
        if (control is CardControl hand) SpatialHandActionStrip.Refresh(hand);
        control.ZIndex = SpatialCardPose.RestingZ(control, pose.Z);
    }
}
