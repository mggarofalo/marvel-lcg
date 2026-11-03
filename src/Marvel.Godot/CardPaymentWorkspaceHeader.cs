using Godot;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Retains the unpaid source, current effect and requirement above payment choices.</summary>
internal static class CardPaymentWorkspaceHeader
{
    internal static void Add(VBoxContainer content, DecisionPanel panel, Main main,
        DecisionComposer draft, PromptPresentation prompt)
    {
        var selected = draft.Selected!;
        AffordancePresentation offer = prompt.Affordances.Single(value => value.Id == selected.Id);
        content.AddChild(DecisionFactsRow(prompt, panel.ReadingScale, selected.DeferredTargetSelection));
        var identity = new HBoxContainer { Name = "PaymentIdentity" };
        content.AddChild(identity);
        BoardCardPresentation? card = CardPaymentPreview.Card(main.boardPresentation, selected.AnchorId);
        if (card is not null)
        {
            CardControl face = CardControl.Create(card, CardDisplaySize.Board, InterfaceScale.Percent50, main.art);
            face.Name = "PlayingCardReference";
            CardInspectorFocus.IgnoreMouseRecursively(face, interactiveRules: false);
            var inspect = new Button { Name = "InspectPaymentPlayed", Flat = true,
                CustomMinimumSize = face.GetCombinedMinimumSize(), SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
                AccessibilityName = $"Inspect {card.Title}" };
            inspect.AddChild(face);
            TooltipPolicy.Inspect(inspect, card.Title);
            inspect.GuiInput += input =>
            {
                if (input is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Enter }) return;
                main.ToggleCardInspector(card, inspect);
                inspect.AcceptEvent();
            };
            CardPaymentInspection.Bind(panel, inspect, selected.AnchorId);
            identity.AddChild(inspect);
        }
        var copy = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.TightStack };
        identity.AddChild(copy);
        copy.AddChild(DecisionPanel.Text($"Play {offer.SourceName ?? offer.Anchor}",
            GodotThemeVariations.Heading, wrap: true));
        if (draft.UsesAutomaticTargetSelection)
            copy.AddChild(DecisionPanel.Text(string.Join(" → ", draft.Targets.Select(target =>
                PromptPresentation.Describe(target, panel.world!))), GodotThemeVariations.Body, wrap: true));
        foreach (string fact in new[] { offer.CostDescription, offer.Description, offer.Consequence }
            .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().Cast<string>())
            copy.AddChild(DecisionPanel.Text(fact, GodotThemeVariations.Body, wrap: true));
        if (selected.CostOptions.Count == 1)
            copy.AddChild(DecisionCostLabel.Create(selected.CostOptions[0], panel.world!, showTarget: false));
    }

    /// <summary>
    /// Who answers, whether the choice is required, its causes and known facts as chips, with the engine's whole
    /// instruction on hover; a deferred target adds its commitment chip.
    /// </summary>
    internal static HFlowContainer DecisionFactsRow(PromptPresentation prompt, InterfaceScale scale, bool deferredTarget = false)
    {
        var row = new HFlowContainer { Name = "DecisionFacts", MouseFilter = Control.MouseFilterEnum.Pass,
            TooltipText = DecisionWording.DecisionDetail(prompt.Frame, prompt.DeclineLabel) };
        row.AddThemeConstantOverride("h_separation", VisualSystem.Spacing(scale).ExtraSmall);
        row.AddThemeConstantOverride("v_separation", VisualSystem.Spacing(scale).ExtraSmall);
        Wording[] wordings = [.. DecisionSheetHead.Facts(prompt.Frame, prompt.DeclineLabel),
            .. deferredTarget ? [DecisionWording.TargetLater] : Array.Empty<Wording>()];
        for (int index = 0; index < wordings.Length; index++)
            row.AddChild(Chip.Create($"DecisionFact{index}", wordings[index], null, scale));
        return row;
    }
}
