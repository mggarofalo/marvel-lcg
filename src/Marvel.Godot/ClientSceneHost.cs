using Godot;

namespace Marvel.Godot;

/// <summary>Finds the table that owns a reparented decision surface.</summary>
internal static class ClientSceneHost
{
    internal static Main MainFor(Node node)
    {
        for (Node? parent = node; parent is not null; parent = parent.GetParent())
            if (parent is Main main) return main;
        throw new InvalidOperationException("The decision surface needs its table host.");
    }

}
