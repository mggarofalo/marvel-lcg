using Godot;

namespace Marvel.Godot;

/// <summary>Removes a prior board surface before an authoritative rerender.</summary>
internal static class BoardRenderCleanup
{
    internal static void Clear(Container container)
    {
        foreach (Node child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }
}
