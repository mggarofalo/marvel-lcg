using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Opens authorized card details without changing an unpaid payment selection.</summary>
internal static class CardPaymentInspection
{
    internal static Button Button(DecisionPanel panel, Main main, int id, string caption = "Inspect")
    {
        BoardCardPresentation? card = main.boardPresentation?.Areas.SelectMany(area => area.Cards)
            .FirstOrDefault(candidate => candidate.TargetId == id && !candidate.Concealed);
        var button = new Button
        {
            Name = $"InspectPayment{id}", Text = caption,
            TooltipText = card is null ? "Card details are not available." : $"Inspect {card.Title} without changing payment.",
            Disabled = card is null, FocusMode = Control.FocusModeEnum.All,
        };
        panel.StyleButton(button, InteractiveVisualState.Resting, compact: true);
        button.AutowrapMode = TextServer.AutowrapMode.Off;
        var draft = panel.composer!;
        int generation = panel.GetRenderGeneration();
        button.Pressed += () =>
        {
            if (card is not null && panel.IsCurrentDraft(draft, generation))
                main.ShowCardInspector(card, button, pinned: true);
        };
        return button;
    }
}
