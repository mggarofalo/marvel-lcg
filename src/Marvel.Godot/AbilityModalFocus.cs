using Godot;

namespace Marvel.Godot;

/// <summary>Keeps deliberate keyboard commitments inside the active ability sheet.</summary>
internal static class AbilityModalFocus
{
    internal static bool Route(Control frame, InputEvent input)
    {
        if (input is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Tab } tab)
            return false;
        Control[] buttons = [.. frame.FindChildren("*", "Control", true, false).OfType<Control>()
            .Where(control => control.IsVisibleInTree() && control.FocusMode == Control.FocusModeEnum.All
                && control is not BaseButton { Disabled: true })];
        frame.GetViewport().SetInputAsHandled();
        if (buttons.Length == 0) return true;
        int current = Array.IndexOf(buttons, frame.GetViewport().GuiGetFocusOwner());
        int next = current < 0 ? 0 : (current + (tab.ShiftPressed ? -1 : 1) + buttons.Length) % buttons.Length;
        buttons[next].GrabFocus();
        frame.GetViewport().SetInputAsHandled();
        return true;
    }
}
