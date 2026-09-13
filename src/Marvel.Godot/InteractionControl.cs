using Godot;

namespace Marvel.Godot;

/// <summary>Validates a re-found control before deferred interaction work uses it.</summary>
internal static class InteractionControl
{
    internal static bool IsUsable(Control? control) =>
        control is not null
        && GodotObject.IsInstanceValid(control)
        && control.IsInsideTree()
        && !control.IsQueuedForDeletion();

    internal static Control? Find(Control root, string? key)
    {
        if (!IsUsable(root) || string.IsNullOrEmpty(key))
        {
            return null;
        }

        Control? candidate = root.FindChild(key, recursive: true, owned: false) as Control;
        return IsUsable(candidate) && root.IsAncestorOf(candidate) ? candidate : null;
    }

    internal static ScrollContainer? ScrollAncestor(Control control, string? name = null)
    {
        if (!IsUsable(control))
        {
            return null;
        }

        for (Node? node = control.GetParent(); node is not null; node = node.GetParent())
        {
            if (node is ScrollContainer scroll
                && (name is null || scroll.Name == name)
                && IsUsable(scroll)
                && scroll.IsAncestorOf(control))
            {
                return scroll;
            }
        }

        return null;
    }
}
