using Godot;

namespace Marvel.Godot;

/// <summary>Previews exact authorized payment sources without changing the draft.</summary>
internal static class CardPaymentInspection
{
    internal static void Bind(DecisionPanel panel, Button button, int id)
    {
        button.SetMeta("payment_inspection", true);
        button.GuiInput += input =>
        {
            if (input is not InputEventKey { Keycode: Key.I, Pressed: true, Echo: false }) return;
            Main main = ClientSceneHost.MainFor(panel);
            if (CardPaymentPreview.Card(main.boardPresentation, id) is not { } card) return;
            main.ToggleCardInspector(card, button);
            button.AcceptEvent();
        };
        button.MouseEntered += () => panel.PreviewPaymentCard(id);
        button.MouseExited += () => panel.PreviewPaymentCard(button.HasFocus() ? id : null);
        CardFocusPreview.Bind(button, () => panel.PreviewPaymentCard(id),
            () => panel.PreviewPaymentCard(button.IsHovered() ? id : null));
    }
}
