using Godot;

namespace Marvel.Godot;

/// <summary>Observes available canvas height as themed ancestor containers settle.</summary>
internal sealed partial class TableCanvasSurface : Control
{
    internal Func<bool>? BoundsChanged { get; set; }
    internal TableCanvasReflow? Reflow { get; set; }

    public override void _Process(double delta)
    {
        if (!IsQueuedForDeletion()) BoundsChanged?.Invoke();
    }
}
