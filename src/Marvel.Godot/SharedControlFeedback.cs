using Godot;

namespace Marvel.Godot;

/// <summary>One bounded interaction signal for authored and runtime controls.</summary>
internal static class SharedControlFeedback
{
    internal static void Install(Control owner)
    {
        if (owner.HasMeta("shared_control_feedback")) return;
        owner.SetMeta("shared_control_feedback", true);
        void Attach()
        {
            SceneTree tree = owner.GetTree();
            void Added(Node node)
            {
                if (node is Button button && (owner == button || owner.IsAncestorOf(button)))
                    Bind(button, owner);
            }
            tree.NodeAdded += Added;
            void Detached()
            {
                tree.NodeAdded -= Added;
                owner.TreeExiting -= Detached;
            }
            owner.TreeExiting += Detached;
            Visit(owner, owner);
        }
        if (owner.IsInsideTree()) Attach();
        else owner.TreeEntered += Attach;
    }

    private static void Visit(Node node, Control owner)
    {
        if (node is Button button) Bind(button, owner);
        foreach (Node child in node.GetChildren()) Visit(child, owner);
    }

    private static void Bind(Button button, Control owner)
    {
        button.ThemeTypeVariation = ButtonHierarchy.Variation(button.Name, button.ThemeTypeVariation);
        if (button.HasMeta("shared_control_bound")) return;
        button.SetMeta("shared_control_bound", true);
        _ = new ButtonSurfaceFeedback(button, owner);
    }
}
