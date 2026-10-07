using Godot;

namespace Marvel.Godot;

/// <summary>Owns visual emphasis for a card being inspected with the pointer.</summary>
internal static class BoardHoverPresentation
{
    internal static void Raise(Control control, int restingZ)
    {
        control.MoveToFront();
        SetControlLayer(control, 400);
        control.ZIndex = Math.Max(140, restingZ + 40);
    }

    internal static void RestoreControls(Control card)
    {
        foreach (Node node in card.FindChildren("Card*", "Button", true, false))
        {
            if (node is Button button && button.HasMeta("spatial_control_z"))
                button.ZIndex = button.GetMeta("spatial_control_z").AsInt32();
        }
    }

    private static void SetControlLayer(Control card, int layer)
    {
        foreach (Node node in card.FindChildren("Card*", "Button", true, false))
        {
            if (node is Button button && button.GetParent()?.Name == "DirectControls")
                button.ZIndex = layer;
        }
    }
}
