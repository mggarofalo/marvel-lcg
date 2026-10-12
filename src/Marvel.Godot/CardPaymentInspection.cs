using Godot;

namespace Marvel.Godot;

/// <summary>Previews exact authorized payment sources without changing the draft.</summary>
internal static class CardPaymentInspection
{
    internal static void Bind(DecisionPanel panel, Button button, int id)
    {
        button.MouseEntered += () => panel.PreviewPaymentCard(id);
        button.MouseExited += () => panel.PreviewPaymentCard(button.HasFocus() ? id : null);
        button.FocusEntered += () => panel.PreviewPaymentCard(id);
        button.FocusExited += () => panel.PreviewPaymentCard(button.IsHovered() ? id : null);
    }
}
