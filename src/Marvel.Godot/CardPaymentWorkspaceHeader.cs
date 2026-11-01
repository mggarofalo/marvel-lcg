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
        content.AddChild(DecisionPanel.Text(prompt.Context(), GodotThemeVariations.Caption, wrap: true));
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
        if (ShowsResolution(draft.Prompt.PublicKind, draft.Prompt.CauseCardIds.Count > 0)
            && !string.IsNullOrWhiteSpace(prompt.Resolution()))
            content.AddChild(DecisionPanel.Text(prompt.Resolution(), GodotThemeVariations.Body, wrap: true));
        if (selected.DeferredTargetSelection)
            content.AddChild(DecisionPanel.Text(
                "Payment commits now. Choose a target as the effect resolves; cancelling a later draft does not refund payment.",
                GodotThemeVariations.Body, wrap: true));
    }

    internal static bool ShowsResolution(PublicDecisionKind kind, bool hasCause) =>
        kind != PublicDecisionKind.PlayerAction || hasCause;
}
