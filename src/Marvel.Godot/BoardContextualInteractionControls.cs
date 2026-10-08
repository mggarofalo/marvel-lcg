using Godot;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns the single task-dock commitment and reversible draft controls.</summary>
internal sealed class BoardContextualInteractionControls(Func<bool> isCurrent)
{
    private Container? host;
    private Action<int>? activate;
    private Action? decline;
    private Action? submit;
    private Action? cancel;
    private Action<int>? selectCost;
    private Func<string>? commitmentLabel;
    private int generation;

    internal void Register(Container container) => host = container;

    internal void Bind(
        Action<int> action, Action pass, Action commit, Action clear,
        Action<int> cost, Func<string> label)
    {
        activate = action;
        decline = pass;
        submit = commit;
        cancel = clear;
        selectCost = cost;
        commitmentLabel = label;
    }

    internal void Clear()
    {
        generation++;
        if (!InteractionControl.IsUsable(host)) return;
        foreach (Node child in host!.GetChildren())
        {
            host.RemoveChild(child);
            child.QueueFree();
        }
    }

    internal void Present(DecisionComposer composer, PromptPresentation prompt,
        IReadOnlySet<int> installedActions)
    {
        if (!InteractionControl.IsUsable(host)) return;
        if (composer.Selected is null)
        {
            if (composer.Prompt.PublicKind == PublicDecisionKind.PlayerAction && composer.Prompt.Cancellable)
                AddDecline(prompt);
            AddActions(prompt, installedActions, composer.Prompt.PublicKind);
        }
        else
        {
            AddDraft(composer);
        }
        if (composer.Selected is null && composer.Prompt.Cancellable
            && composer.Prompt.PublicKind != PublicDecisionKind.PlayerAction)
            AddDecline(prompt);
    }

    private void AddDecline(PromptPresentation prompt) =>
        Add("ContextualDecline", prompt.DeclineLabel, () => decline?.Invoke(), prompt.DeclineLabel);

    internal static IReadOnlyList<AffordancePresentation> FallbackActions(
        PromptPresentation prompt, IReadOnlySet<int> installedActions,
        PublicDecisionKind kind = PublicDecisionKind.PlayerAction) =>
        [.. prompt.Affordances.Where(candidate => candidate.Illegal is null
            && (kind is PublicDecisionKind.Choice or PublicDecisionKind.Response
                || candidate.CardAnchorId is not { } anchor || !installedActions.Contains(anchor)))];

    private void AddActions(PromptPresentation prompt, IReadOnlySet<int> installedActions, PublicDecisionKind kind)
    {
        IReadOnlyList<AffordancePresentation> offers = FallbackActions(prompt, installedActions, kind);
        Container choices = host!;
        if (kind == PublicDecisionKind.Choice && offers.Count is 2 or 3)
        {
            choices = new HBoxContainer
            {
                Name = "RequiredAlternatives", ThemeTypeVariation = GodotThemeVariations.TightStack,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            host!.AddChild(choices);
        }
        foreach (AffordancePresentation offer in offers)
            AddAction(choices, offer, prompt, kind);
    }

    private void AddAction(Container choices, AffordancePresentation offer,
        PromptPresentation prompt, PublicDecisionKind kind)
    {
        int id = offer.Id;
        string label = kind == PublicDecisionKind.Choice
            ? offer.DisplayLabel ?? DecisionCopy.Choice(offer) : DecisionCopy.Choice(offer);
        string? description = kind is not (PublicDecisionKind.Choice or PublicDecisionKind.PlayerAction)
            && ContextualOfferDescription.SingleResponse(prompt, kind) is null ? offer.Description : null;
        Container row = ActionRow(choices, description);
        Button choice = Add($"ContextAction{id}", label,
            () => activate?.Invoke(id), offer.Description ?? offer.Label, parent: row);
        DecisionCostLabel.AttachTo(choice, offer);
        if (!string.IsNullOrEmpty(description)) row.AddChild(new Label
        {
            Text = description, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.Caption,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
    }

    private static Container ActionRow(Container choices, string? description)
    {
        if (string.IsNullOrEmpty(description)) return choices;
        var row = new VBoxContainer
        {
            ThemeTypeVariation = GodotThemeVariations.TightStack,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        choices.AddChild(row);
        return row;
    }

    private void AddDraft(DecisionComposer composer)
    {
        AddCostChoices(composer);
        DecisionProgressPresentation progress = composer.Progress();
        string label = DecisionCopy.WithPaymentConsequence(
            commitmentLabel?.Invoke() ?? composer.Selected!.Label, progress.Payment);
        var commitments = new HBoxContainer
        {
            Name = "DraftCommitments", ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        host!.AddChild(commitments);
        Add("ContextualCommit", label, () => submit?.Invoke(), label, !progress.IsReady, commitments);
        if (CanClearDraft(composer))
            Add("ContextualCancelDraft", CancelLabel(composer.Prompt), () => cancel?.Invoke(),
                "Clear these uncommitted choices. Accepted actions remain in play.", parent: commitments);
    }

    internal static bool CanClearDraft(DecisionComposer composer) =>
        !(MulliganPrompt.IsOpening(composer.Prompt) || InitialTableDraft.IsRequiredEndPhase(composer.Prompt))
        || composer.Targets.Count > 0;

    private static string CancelLabel(Prompt prompt) => MulliganPrompt.IsOpening(prompt)
        ? "Clear replacements"
        : InitialTableDraft.IsRequiredEndPhase(prompt) ? "Clear discards" : "Cancel draft";

    private void AddCostChoices(DecisionComposer composer)
    {
        if (composer.Selected is not { CostOptions.Count: > 0 } selected) return;
        if (selected.CostOptions.Count == 1)
        {
            host!.AddChild(DecisionCostLabel.Requirements(selected.CostOptions[0]));
            return;
        }
        for (int index = 0; index < selected.CostOptions.Count; index++)
        {
            int option = index;
            CostOption cost = selected.CostOptions[index];
            Button choice = Add($"ContextualCost{index}", "", () => selectCost?.Invoke(option),
                DecisionCostLabel.Accessible(cost), !composer.CostApplies(cost));
            choice.ToggleMode = true;
            choice.ButtonPressed = composer.SelectedCost == index;
            HBoxContainer row = ResourceIconRendering.Row($"Pay {cost.Cost}",
                string.Concat(cost.Rule ?? []), GodotThemeVariations.Caption);
            if (cost.HasAlternative)
                row.AddChild(ResourceIconRendering.Row($"or {cost.OrCost}",
                    string.Concat(cost.OrRule ?? []), GodotThemeVariations.Caption));
            ResourceIconRendering.ButtonContent(choice, row, DecisionCostLabel.Accessible(cost));
        }
    }

    private Button Add(string name, string text, Action action, string tooltip, bool disabled = false, Container? parent = null)
    {
        var button = new Button
        {
            Name = name, Text = text, TooltipText = tooltip, AccessibilityName = text,
            FocusMode = Control.FocusModeEnum.All, Disabled = disabled,
            CustomMinimumSize = new Vector2(0, 44),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.LegalTargetButton,
        };
        int current = generation;
        button.Pressed += () =>
        {
            if (isCurrent() && current == generation && !button.Disabled) action();
        };
        (parent ?? host!).AddChild(button);
        TableCompactButtonStyle.Apply(button);
        return button;
    }
}
