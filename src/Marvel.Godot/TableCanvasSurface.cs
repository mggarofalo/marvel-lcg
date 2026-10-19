using Godot;

namespace Marvel.Godot;

/// <summary>Reports settled table placement when its themed ancestors move.</summary>
internal sealed partial class TableCanvasSurface : Control
{
    internal Action? BoundsChanged { get; set; }

    public override void _Ready() => SetNotifyTransform(true);

    public override void _Notification(int what)
    {
        if (what != NotificationTransformChanged && what != NotificationResized) return;
        Callable.From(() =>
        {
            if (IsInsideTree() && !IsQueuedForDeletion()) BoundsChanged?.Invoke();
        }).CallDeferred();
    }
}
