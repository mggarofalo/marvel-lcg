using Godot;

namespace Marvel.Godot;

/// <summary>Old physical cards remain visible, but cannot route a gesture into a newer decision.</summary>
internal static class StagedResponseInput
{
    internal static bool Consume(Main main, InputEvent input)
    {
        // A pinned inspector owns its complete dismissal gesture, including the backdrop release.
        if (main.cardInspector.Visible && main.cardInspectorPinned) return false;
        if (input is InputEventKey { Keycode: Key.Escape or Key.Tab }) return false;
        Control? target = input is InputEventKey ? main.GetViewport().GuiGetFocusOwner()
            : main.GetViewport().GuiGetHoveredControl();
        if (target?.HasMeta("accepted_decision_entry") == true) return false;
        if (!IsStagedBoardTarget(main, target)) return false;
        Inspect(main, FindCard(target), input);
        if (input is InputEventMouseButton or InputEventKey) main.GetViewport().SetInputAsHandled();
        return true;
    }

    private static bool IsStagedBoardTarget(Main main, Control? target)
    {
        Control table = main.GetNode<Control>("Margin/Shell/Content/Play/Board");
        if (target is null || !table.IsAncestorOf(target)) return false;
        for (Node? node = target; node is not null; node = node.GetParent())
            if (node.Name == "EventTimeline") return false;
        return true;
    }

    private static void Inspect(Main main, CardControl? card, InputEvent input)
    {
        if (card?.DisplayedPresentation is not { Concealed: false } shown || card.HasMeta("activity_departed"))
        {
            if (input is InputEventMouseMotion) main.boardController.ScheduleCardInspectorHide();
            return;
        }
        bool pin = input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
            or InputEventKey { Pressed: true, Keycode: Key.Enter };
        if (pin || ShouldHover(main, input, shown.TargetId))
            main.boardController.ShowCardInspector(shown, card, pin);
    }

    private static bool ShouldHover(Main main, InputEvent input, int? id) =>
        input is InputEventMouseMotion && (!main.cardInspector.Visible || main.inspectedCardId != id);

    private static CardControl? FindCard(Node? node)
    {
        for (; node is not null; node = node.GetParent())
            if (node is CardControl card) return card;
        return null;
    }
}
