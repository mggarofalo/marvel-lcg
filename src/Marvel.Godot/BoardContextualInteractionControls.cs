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
    private Vector2? releaseAnchor;
    private int? releaseOffer;
    private bool local;

    internal void AnchorDraft(Vector2 position, int offer) { releaseAnchor = position; releaseOffer = offer; }
    internal void SetVisible(bool visible)
    {
        if (host?.GetNodeOrNull<CanvasLayer>("TargetCommitmentLayer") is { } layer) layer.Visible = visible;
    }

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
            releaseAnchor = null;
            if (composer.Prompt.PublicKind == PublicDecisionKind.PlayerAction && composer.Prompt.Cancellable)
                AddDecline(prompt);
            AddActions(prompt, installedActions, composer.Prompt.PublicKind);
        }
        else
        {
            PresentDraft(composer, prompt);
        }
        if (composer.Selected is null && composer.Prompt.Cancellable
            && composer.Prompt.PublicKind != PublicDecisionKind.PlayerAction)
            AddDecline(prompt);
    }

    private void PresentDraft(DecisionComposer composer, PromptPresentation prompt)
    {
        if (composer.Selected?.Id != releaseOffer) releaseAnchor = null;
        if (releaseAnchor is not { } point) { AddDraft(composer); return; }
        Container dock = host!;
        var localLayer = new CanvasLayer { Name = "TargetCommitmentLayer", Layer = 80 };
        dock.AddChild(localLayer);
        var panel = new PanelContainer { Name = "TargetCommitment", Theme = ThemeFor(dock),
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel,
            CustomMinimumSize = new Vector2(420, 0), Size = new Vector2(420, 1) };
        localLayer.AddChild(panel);
        var column = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        panel.AddChild(column);
        var visible = prompt.Affordances
            .FirstOrDefault(offer => offer.Id == composer.Selected!.Id);
        if (visible is not null) column.AddChild(DecisionPanel.Text(DecisionCopy.ActionSummary(visible),
            GodotThemeVariations.Body, wrap: true));
        foreach (int target in composer.Targets)
        {
            if (visible?.TargetRequest?.Details?.GetValueOrDefault(target) is { Length: > 0 } detail)
                column.AddChild(DecisionPanel.Text(detail, GodotThemeVariations.Body, wrap: true));
        }
        host = column;
        local = true;
        AddDraft(composer);
        local = false;
        host = dock;
        Callable.From(() =>
        {
            if (!InteractionControl.IsUsable(panel)) return;
            panel.Size = new Vector2(420, panel.GetCombinedMinimumSize().Y);
            Vector2 viewport = panel.GetViewportRect().Size;
            panel.Position = new Vector2(Math.Clamp(point.X + 14, 8, Math.Max(8, viewport.X - panel.Size.X - 8)),
                Math.Clamp(point.Y + 14, 8, Math.Max(8, viewport.Y - panel.Size.Y - 8)));
        }).CallDeferred();
    }

    private static Theme? ThemeFor(Node node)
    {
        for (Node? current = node; current is not null; current = current.GetParent())
            if (current is Control { Theme: { } theme }) return theme;
        return null;
    }

    private void AddDecline(PromptPresentation prompt)
    {
        Button button = Add("ContextualDecline", prompt.DeclineLabel, () => decline?.Invoke(), prompt.DeclineLabel);
        button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        button.CustomMinimumSize = new Vector2(160, 44);
    }

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
        DecisionCostLabel.AttachTo(choice, offer, compact: true);
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
        Container dock = host!;
        if (!local && composer.Selected?.CostOptions.Count == 1)
        {
            var inline = new HBoxContainer
            {
                Name = "InlineCostCommitment", ThemeTypeVariation = GodotThemeVariations.TightStack,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            dock.AddChild(inline);
            host = inline;
        }
        AddCostChoices(composer);
        DecisionProgressPresentation progress = composer.Progress();
        string label = CommitmentLabel(composer, progress);
        var commitments = new HBoxContainer
        {
            Name = "DraftCommitments", ThemeTypeVariation = GodotThemeVariations.TightStack,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(280, 44),
        };
        host!.AddChild(commitments);
        Add("ContextualCommit", local ? "✓ " + label : label, () => submit?.Invoke(), label, !progress.IsReady, commitments);
        if (CanClearDraft(composer))
            Add("ContextualCancelDraft", local ? "× Cancel" : CancelLabel(composer.Prompt), () => cancel?.Invoke(),
                "Clear these uncommitted choices. Accepted actions remain in play.", parent: commitments);
        host = dock;
    }

    private string CommitmentLabel(DecisionComposer composer, DecisionProgressPresentation progress) =>
        DecisionCopy.WithPaymentConsequence(
            commitmentLabel?.Invoke() ?? composer.Selected!.Label, progress.Payment);

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
            ThemeTypeVariation = name == "ContextualCommit" ? GodotThemeVariations.PrimaryButton
                : name == "ContextualCancelDraft" ? GodotThemeVariations.TertiaryButton : GodotThemeVariations.ChoiceButton,
        };
        int current = generation;
        button.SetMeta("table_mutation", true);
        button.Pressed += () =>
        {
            if (isCurrent() && current == generation && !button.Disabled) action();
        };
        (parent ?? host!).AddChild(button);
        TableCompactButtonStyle.Apply(button);
        return button;
    }
}
