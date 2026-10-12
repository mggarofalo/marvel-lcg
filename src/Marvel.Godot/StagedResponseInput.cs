using Godot;

namespace Marvel.Godot;

/// <summary>Old physical cards remain visible, but cannot route a gesture into a newer decision.</summary>
internal static class StagedResponseInput
{
    internal static bool Consume(Main main, InputEvent input)
    {
        // Let Godot update hover ownership so review, Sync and Undo remain reachable.
        if (input is InputEventMouseMotion) return true;
        Control? target = input is InputEventKey ? main.GetViewport().GuiGetFocusOwner()
            : main.GetViewport().GuiGetHoveredControl();
        Control table = main.GetNode<Control>("Margin/Shell/Content/Play/Board");
        if (target is null || !table.IsAncestorOf(target)) return false;
        for (Node? node = target; node is not null; node = node.GetParent())
            if (node.Name == "EventTimeline") return false;
        if (input is not (InputEventMouseButton or InputEventKey)) return false;
        main.GetViewport().SetInputAsHandled();
        return true;
    }
}
