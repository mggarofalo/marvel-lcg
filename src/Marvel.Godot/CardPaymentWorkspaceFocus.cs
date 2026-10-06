using Godot;

namespace Marvel.Godot;

/// <summary>Distinguishes payment keyboard intent from inspection elsewhere on the table.</summary>
internal static class CardPaymentWorkspaceFocus
{
    internal static bool Contains(Control workspace, Control? focused) =>
        InteractionControl.IsUsable(focused) && (focused == workspace || workspace.IsAncestorOf(focused));
}
