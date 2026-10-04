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
            AddActions(prompt, installedActions);
        }
        else
        {
            AddDraft(composer);
        }
        if (composer.Prompt.Cancellable)
            Add("ContextualDecline", prompt.DeclineLabel, () => decline?.Invoke(), prompt.DeclineLabel);
    }

    internal static IReadOnlyList<AffordancePresentation> FallbackActions(
        PromptPresentation prompt, IReadOnlySet<int> installedActions) =>
        [.. prompt.Affordances.Where(candidate => candidate.Illegal is null
            && (candidate.CardAnchorId is not { } anchor || !installedActions.Contains(anchor)))];

    private void AddActions(PromptPresentation prompt, IReadOnlySet<int> installedActions)
    {
        foreach (AffordancePresentation offer in FallbackActions(prompt, installedActions))
        {
            int id = offer.Id;
            Add($"ContextAction{id}", DecisionCopy.Choice(offer),
                () => activate?.Invoke(id), offer.Description ?? offer.Label);
            if (offer.Description is { Length: > 0 } description)
                host!.AddChild(new Label
                {
                    Text = description, AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    ThemeTypeVariation = GodotThemeVariations.Caption,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                });
        }
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
        Add("ContextualCancelDraft", CancelLabel(composer.Prompt), () => cancel?.Invoke(),
            "Clear these uncommitted choices. Accepted actions remain in play.", parent: commitments);
    }

    private static string CancelLabel(Prompt prompt) => MulliganPrompt.IsOpening(prompt)
        ? "Clear replacements"
        : InitialTableDraft.IsRequiredEndPhase(prompt) ? "Clear discards" : "Cancel draft";

    private void AddCostChoices(DecisionComposer composer)
    {
        if (composer.Selected is not { CostOptions.Count: > 1 } selected) return;
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
