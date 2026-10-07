using Godot;
using Marvel.Decisions;

namespace Marvel.Godot;

/// <summary>Restores lost card focus after rebuilding a draft without overriding later input.</summary>
internal static class BoardControlFocusRestore
{
    internal static void Restore(
        DecisionPanel panel,
        DecisionComposer composer,
        int generation,
        int cardId)
    {
        if (panel.PaymentModalOpen || !panel.IsCurrentDraft(composer, generation)) return;
        Control? current = panel.GetViewport().GuiGetFocusOwner();
        if (InteractionControl.IsUsable(current) && current!.IsVisibleInTree()) return;
        Button? candidate = Candidate(panel.GetTree().Root, cardId);
        if (candidate is not null)
        {
            candidate.GrabFocus();
            InteractionControl.ResetDisabledScrollAncestors(candidate);
        }
    }
    private static Button? Candidate(Node root, int cardId)
    {
        Button[] sameCard = [.. root
            .FindChildren($"Card{cardId}*", "Button", true, false)
            .OfType<Button>()
            .Where(CanFocus)];
        return sameCard.LastOrDefault() ?? root
            .FindChildren("Card*", "Button", true, false)
            .OfType<Button>()
            .LastOrDefault(button => CanFocus(button) && button.HasMeta("spatial_card_anchor"))
            ?? root.FindChild("ContextualCommit", true, false) as Button;
    }

    private static bool CanFocus(Button button) =>
        InteractionControl.IsUsable(button) && button.IsVisibleInTree() && !button.Disabled;

}
