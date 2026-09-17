using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns modal inspector focus, dismissal, and stable-source restoration.</summary>
internal sealed class CardInspectorFocus
{
    private readonly Main main;
    private int? returnTargetId;
    private bool backdropDismissalPending;

    internal CardInspectorFocus(Main main)
    {
        this.main = main;
    }

    internal void FocusDetail(int generation)
    {
        if (!InteractionControl.IsUsable(main)
            || generation != main.cardInspectorGeneration
            || !InteractionControl.IsUsable(main.cardInspector)
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
        if (CompleteBackdropDismissal(input))
        {
            return;
        }
        if (CycleModalFocus(input) || DismissWithKeyboard(input))
        {
            return;
        }
        if (OutsideClick(input) && main.cardInspectorPinned)
        {
            backdropDismissalPending = true;
            main.GetViewport().SetInputAsHandled();
        }
    }

    private bool CompleteBackdropDismissal(InputEvent input)
    {
        if (!backdropDismissalPending
            || input is not InputEventMouseButton
                { ButtonIndex: MouseButton.Left, Pressed: false }) return false;
        backdropDismissalPending = false;
        main.GetViewport().SetInputAsHandled();
        int framesRemaining = 2;
        void HideAfterInputFrame()
        {
            framesRemaining--;
            if (framesRemaining > 0) return;
            main.GetTree().ProcessFrame -= HideAfterInputFrame;
            Hide();
        }
        main.GetTree().ProcessFrame += HideAfterInputFrame;
        return true;
    }

    private bool CycleModalFocus(InputEvent input)
    {
        if (!main.cardInspector.Visible || !main.cardInspectorPinned
            || input is not InputEventKey { Keycode: Key.Tab, Pressed: true } tab) return false;
        CycleFocus(tab.ShiftPressed);
        main.GetViewport().SetInputAsHandled();
        return true;
    }

    private bool DismissWithKeyboard(InputEvent input)
    {
        if (!main.cardInspector.Visible || !input.IsActionPressed("ui_cancel")) return false;
        Hide();
        main.GetViewport().SetInputAsHandled();
        return true;
    }

    internal void ScheduleHide()
    {
        int generation = ++main.cardInspectorGeneration;
        main.GetTree().CreateTimer(0.3).Timeout += () =>
        {
            if (InteractionControl.IsUsable(main)
                && generation == main.cardInspectorGeneration
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
        backdropDismissalPending = false;
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

    internal static void IgnoreMouseRecursively(Node node, bool interactiveRules = true)
    {
        if (interactiveRules && node is RichTextLabel rules)
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
            IgnoreMouseRecursively(child, interactiveRules);
        }
    }

    internal static void RestoreMouseRecursively(Node node)
    {
        if (node is RichTextLabel rules)
        {
            rules.MouseFilter = Control.MouseFilterEnum.Stop;
            return;
        }
        if (node is Control control)
        {
            control.MouseFilter = Control.MouseFilterEnum.Pass;
        }
        foreach (Node child in node.GetChildren())
        {
            RestoreMouseRecursively(child);
        }
    }

    private bool OutsideClick(InputEvent input) => main.cardInspector.Visible
        && input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click
        && !main.cardInspectorFrame.GetGlobalRect().HasPoint(click.Position);

    private void CycleFocus(bool reverse)
    {
        var candidates = new List<Control>();
        if (main.cardInspectorContent.GetChildCount() > 0
            && main.cardInspectorContent.GetChild(0) is Control detail)
        {
            candidates.Add(detail);
        }
        candidates.AddRange(main.cardInspectorFrame
            .FindChildren("*", "Button", true, false)
            .OfType<Button>()
            .Where(button => button.Visible && !button.Disabled));
        if (candidates.Count == 0) return;
        Control? focused = main.GetViewport().GuiGetFocusOwner();
        int current = candidates.IndexOf(focused!);
        int offset = reverse ? -1 : 1;
        int next = current < 0
            ? 0
            : (current + offset + candidates.Count) % candidates.Count;
        candidates[next].GrabFocus();
    }

    private void RestoreSource(int targetId, int generation)
    {
        if (InteractionControl.IsUsable(main)
            && generation == main.cardInspectorGeneration && !main.cardInspector.Visible
            && main.boardRender?.ControlFor(targetId) is Control source
            && InteractionControl.IsUsable(source))
        {
            source.GrabFocus();
            InteractionControl.ResetDisabledScrollAncestors(source);
        }
    }

}
