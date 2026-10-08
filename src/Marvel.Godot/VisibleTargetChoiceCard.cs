using Godot;
using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Separates inspecting a visible candidate from toggling its physical copy in the draft.</summary>
internal static class VisibleTargetChoiceCard
{
    internal static void Add(DecisionPanel panel, HBoxContainer row, int target,
        BoardCardPresentation? card, InterfaceScale scale, SearchChoiceInspection inspection, TableDraftBinding operations)
    {
        DecisionComposer draft = panel.composer!;
        bool selected = draft.Targets.Contains(target);
        bool permitted = operations.CanToggleTarget(target);
        string title = card?.Title ?? "Card details unavailable";
        var choose = new Button
        {
            Name = $"Target{target}", Text = selected ? "✓" : "◎", ToggleMode = true, ButtonPressed = selected,
            AccessibilityName = selected ? $"Unselect {title}" : $"Select {title}",
            Disabled = panel.submitting || !permitted, CustomMinimumSize = new Vector2(44, 44),
        };
        choose.TooltipText = permitted ? choose.AccessibilityName : "Remove a selected card to choose another combination.";
        choose.Pressed += () =>
        {
            if (!operations.TryToggleTarget(target)) return;
            panel.NotifyAnchorFocused([target]);
            panel.Rebuild();
        };
        panel.BindAnchors(choose, target);
        if (card is null)
        {
            choose.Text = title;
            row.AddChild(choose);
            return;
        }
        CardControl face = CardControl.Create(card, CardDisplaySize.Full, scale, ClientSceneHost.MainFor(panel).art);
        face.Name = $"VisibleTargetCard{target}";
        inspection.Bind(face, card);
        row.AddChild(CardStateDetails.Wrap(face, card, beside: false));
        face.SetInteractionCue(selected ? CardInteractionCue.SelectedTarget : CardInteractionCue.LegalTarget);
        face.HideInteractionCue();
        face.GetNode<Control>("CardSurface").AddChild(choose);
        choose.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        choose.Position = new Vector2(face.Size.X - 48, 4);
        CardSymbolButtonStyle.Apply(choose);
    }
}
