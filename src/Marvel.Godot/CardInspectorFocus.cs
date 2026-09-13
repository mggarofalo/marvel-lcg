using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns modal inspector focus, dismissal, and stable-source restoration.</summary>
internal sealed class CardInspectorFocus
{
    private readonly Main main;
    private int? returnTargetId;

    internal CardInspectorFocus(Main main)
    {
        this.main = main;
    }

    internal void FocusDetail(int generation)
    {
        if (generation != main.cardInspectorGeneration || !InteractionControl.IsUsable(main.cardInspector)
            || !main.cardInspector.Visible || main.cardInspectorContent.GetChildCount() != 1
            || main.cardInspectorContent.GetChild(0) is not Control detail
            || !InteractionControl.IsUsable(detail) || !main.cardInspector.IsAncestorOf(detail))
        {
            return;
        }

        detail.GrabFocus();
    }

    internal void RememberSource(int? targetId) => returnTargetId = targetId;

    internal void Input(InputEvent input)
    {
        if (main.cardInspector.Visible && main.cardInspectorPinned
            && input is InputEventKey { Keycode: Key.Tab, Pressed: true })
        {
            if (main.cardInspectorContent.GetChildCount() > 0
                && main.cardInspectorContent.GetChild(0) is Control detail)
            {
                detail.GrabFocus();
            }
            main.GetViewport().SetInputAsHandled();
            return;
        }

        if (main.cardInspector.Visible && input.IsActionPressed("ui_cancel"))
        {
            Hide();
            main.GetViewport().SetInputAsHandled();
            return;
        }

        if (OutsideClick(input))
        {
            Hide();
            main.GetViewport().SetInputAsHandled();
        }
    }

    internal void ScheduleHide()
    {
        int generation = ++main.cardInspectorGeneration;
        main.GetTree().CreateTimer(0.3).Timeout += () =>
        {
            if (generation == main.cardInspectorGeneration && main.IsInsideTree()
                && InteractionControl.IsUsable(main.cardInspector) && !main.cardInspectorPinned
                && !main.cardInspectorHovered && !HasFocus())
            {
                main.cardInspectorFrame.FocusMode = Control.FocusModeEnum.None;
                main.cardInspectorScroll.FocusMode = Control.FocusModeEnum.None;
                main.inspectedCardId = null;
                main.cardInspector.Visible = false;
            }
        };
    }

    internal void BindFocus(Control control)
    {
        control.FocusEntered += () => main.cardInspectorGeneration++;
        control.FocusExited += ScheduleHide;
    }

    internal bool HasFocus()
    {
        Control? focused = main.GetViewport()?.GuiGetFocusOwner();
        return InteractionControl.IsUsable(focused)
            && (focused == main.cardInspectorFrame || main.cardInspectorFrame.IsAncestorOf(focused));
    }

    internal void Hide()
    {
        int? targetId = main.cardInspectorPinned ? returnTargetId : null;
        int generation = checked(++main.cardInspectorGeneration);
        main.cardInspectorPinned = false;
        main.cardInspectorHovered = false;
        main.cardInspectorFrame.FocusMode = Control.FocusModeEnum.None;
        main.cardInspectorScroll.FocusMode = Control.FocusModeEnum.None;
        main.inspectedCardId = null;
        returnTargetId = null;
        main.cardInspector.Visible = false;
        if (targetId is not null)
        {
            Callable.From(() => RestoreSource(targetId.Value, generation)).CallDeferred();
        }
    }

    internal static InterfaceScale FittedScale(
        BoardCardPresentation card,
        InterfaceScale requested,
        float viewportHeight)
    {
        int baseHeight = VisualSystem.CardFrame(card.Kind).Family == CardFrameFamily.Scheme ? 400 : 560;
        int available = (int)MathF.Floor(Math.Max(1, viewportHeight - 48) * 100 / baseHeight / 10) * 10;
        return (InterfaceScale)Math.Clamp(Math.Min((int)requested, available), 50, 150);
    }

    internal static bool IsInsideCard(Node? node)
    {
        for (Node? current = node; current is not null; current = current.GetParent())
        {
            if (current is CardControl)
            {
                return true;
            }
        }
        return false;
    }

    internal static void IgnoreMouseRecursively(Node node)
    {
        if (node is RichTextLabel rules)
        {
            rules.MouseFilter = Control.MouseFilterEnum.Stop;
            return;
        }
        if (node is Control control)
        {
            control.MouseFilter = Control.MouseFilterEnum.Ignore;
        }
        foreach (Node child in node.GetChildren())
        {
            IgnoreMouseRecursively(child);
        }
    }

    private bool OutsideClick(InputEvent input) => main.cardInspector.Visible && main.cardInspectorPinned
        && input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click
        && !main.cardInspectorFrame.GetGlobalRect().HasPoint(click.Position);

    private void RestoreSource(int targetId, int generation)
    {
        if (generation == main.cardInspectorGeneration && !main.cardInspector.Visible
            && main.boardRender?.ControlFor(targetId) is Control source
            && InteractionControl.IsUsable(source))
        {
            source.GrabFocus();
        }
    }
}
