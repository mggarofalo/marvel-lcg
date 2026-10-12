using Godot;

namespace Marvel.Godot;

/// <summary>Underlined values disclose their supplied calculation on demand.</summary>
internal static class ModifiedValueDisclosure
{
    internal static void Bind(CardControl face, string key, VBoxContainer details, Control section)
    {
        Control? value = key == "HP"
            ? face.FindChild("ModifiedHealthMaximum", true, false)?.GetParent<Control>()
            : face.FindChild("Stat" + key, true, false) as Control;
        if (value is null) return;
        value.MouseFilter = Control.MouseFilterEnum.Stop;
        value.FocusMode = Control.FocusModeEnum.All;
        value.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        value.TooltipText = $"Explain modified {key}";
        value.AccessibilityName = value.TooltipText;
        value.FocusEntered += value.QueueRedraw;
        value.FocusExited += value.QueueRedraw;
        value.Draw += () =>
        {
            if (value.HasFocus()) value.DrawRect(new Rect2(Vector2.Zero, value.Size),
                ClientTheme.ToGodot(CardVisualTokens.Modified), filled: false, width: 2);
        };
        void Toggle()
        {
            bool open = !section.Visible;
            foreach (Control sibling in details.GetChildren().OfType<Control>()) sibling.Visible = false;
            section.Visible = open;
            details.Visible = open;
        }
        value.GuiInput += input =>
        {
            bool pointer = input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true };
            if (!pointer && !input.IsActionPressed("ui_accept")) return;
            Toggle();
            value.AcceptEvent();
        };
    }
}
