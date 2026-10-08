using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Builds a full candidate face and its explicit prompt selection control.</summary>
internal static class SearchChoiceCard
{
    internal static void Add(DecisionPanel panel, HBoxContainer row, AffordancePresentation option,
        BoardCardPresentation? card, int generation, InterfaceScale scale, SearchChoiceInspection inspection)
    {
        bool selected = panel.composer!.Selected?.Id == option.Id;
        var choice = new Button
        {
            Name = $"Affordance{option.Id}", Text = selected ? "✓" : "◎",
            AccessibilityName = $"Select {option.SourceName ?? card?.Title ?? option.Label}",
            Disabled = panel.submitting || option.Illegal is not null,
            CustomMinimumSize = new Vector2(44, 44),
        };
        choice.TooltipText = choice.AccessibilityName;
        choice.Pressed += () => panel.SelectAffordance(option.Id, generation);
        if (card is null)
        {
            choice.Text = option.DisplayLabel ?? option.Label;
            row.AddChild(choice);
            return;
        }
        CardControl face = CardControl.Create(card, CardDisplaySize.Full, scale, ClientSceneHost.MainFor(panel).art);
        face.Name = $"SearchResult{option.Id}";
        inspection.Bind(face, card);
        row.AddChild(CardStateDetails.Wrap(face, card, beside: false));
        face.SetInteractionCue(selected ? CardInteractionCue.SelectedTarget : CardInteractionCue.LegalTarget);
        face.HideInteractionCue();
        face.GetNode<Control>("CardSurface").AddChild(choice);
        choice.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        choice.Position = new Vector2(face.Size.X - 48, 4);
        CardSymbolButtonStyle.Apply(choice);
    }

}
