using Godot;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders target selection, costs, and submission for one decision draft.</summary>
internal sealed class DecisionDraftRenderer
{
    private readonly DecisionPanel panel;
    private readonly DecisionComposer composer;
    private readonly WorldDescriptor world;
    private readonly bool submitting;
    private readonly int generation;
    private readonly TableDraftBinding operations;

    internal DecisionDraftRenderer(
        DecisionPanel panel,
        DecisionComposer composer,
        WorldDescriptor world,
        bool submitting,
        int generation)
    {
        this.panel = panel;
        this.composer = composer;
        this.world = world;
        this.submitting = submitting;
        this.generation = generation;
        operations = panel.BindTableDraft(composer, generation);
    }
    internal void AddTargets(Affordance selected, TargetSelectionProgress progress)
    {
        TargetRequest? request = selected.Targets;
        if (request is null)
        {
            return;
        }

        if (composer!.UsesAutomaticTargetSelection)
        {
            Label automatic = DecisionPanel.Text(
                string.Join(" → ", composer.Targets.Select(id =>
                    PromptPresentation.Describe(id, world!))),
                GodotThemeVariations.Caption,
                wrap: true);
            automatic.Name = "AutomaticTargets";
            panel.BindAnchors(automatic, [.. composer.Targets]);
            AddContent(automatic);
            return;
        }

        AddProgress(progress);
        AddTargetInstructions(request);

        if (request.IsGrouped)
        {
            AddGroupedTargets(request);
            return;
        }

        AddLegalTargets(request);
        AddTargetOrder();
    }

    private void AddContent(Control control)
    {
        if (panel.PaymentModal is { } payment && composer.Selected?.Targets?.Legal.Count <= 3)
            payment.Targets.AddChild(control);
        else panel.AddContent(control);
    }

    // The sheet's summary row already tracks the selection, so the sheet leaves this tracker out.
    private void AddProgress(TargetSelectionProgress progress)
    {
        if (panel.CompleteChoicesOpen || ChoiceRowWording.Progress(composer, progress) is not { } part) return;
        HFlowContainer row = DraftFactsRow.Create(panel.ReadingScale);
        row.Name = "TargetProgress";
        DraftFactsRow.Fill(row, [part]);
        AddContent(row);
    }

    private void AddTargetInstructions(TargetRequest request)
    {
        if (request.MustIncludeTraits is { Count: > 0 })
        {
            string traits = string.Join(", ", request.MustIncludeTraits);
            AddContent(Chip.Create("MustInclude", new Wording("", $"Must include {traits}",
                $"The selection must include a {traits} card."), null, panel.ReadingScale));
        }
    }

    private void AddGroupedTargets(TargetRequest request)
    {
        for (int index = 0; index < request.Groups!.Count; index++)
        {
            IReadOnlyList<int> group = request.Groups[index];
            var choose = new Button
            {
                Name = $"Group{index}",
                Text = ChoiceRowWording.Group(index, group.Select(id => PromptPresentation.Describe(id, world)),
                    composer.Targets.SequenceEqual(group)),
                Alignment = HorizontalAlignment.Left,
                ToggleMode = true,
                ButtonPressed = composer.Targets.SequenceEqual(group),
                Disabled = submitting,
            };
            panel.StyleButton(choose, choose.ButtonPressed
                ? InteractiveVisualState.Selected
                : InteractiveVisualState.Legal);
            choose.Pressed += () =>
            {
                if (operations.TrySelectGroup(group)) panel.Rebuild();
            };
            panel.BindAnchors(choose, [.. group]);
            AddContent(choose);
        }
    }

    private void AddLegalTargets(TargetRequest request)
    {
        foreach (int target in request.Legal.Distinct())
        {
            if (request.AllowRepeated)
            {
                AddRepeatedTarget(request, target);
            }
            else
            {
                AddOrdinaryTarget(target);
            }
        }
    }

    private void AddTargetOrder()
    {
        if (composer.Targets.Count > 1)
        {
            string order = string.Join(" → ", composer.Targets.Select(id => PromptPresentation.Describe(id, world)));
            AddContent(Chip.Create("TargetOrder", new Wording("", order, $"Order of resolution: {order}."),
                null, panel.ReadingScale));
        }
    }

    internal void AddOrdinaryTarget(int target)
    {
        string targetName = PromptPresentation.Describe(target, world!);
        TargetRequest request = composer!.Selected!.Targets!;
        string? detail = ChoiceRowWording.Distinguishing(request, target, targetName,
            panel.PaymentModalOpen ? composer.Selected?.Description : null);
        bool selected = composer.Targets.Contains(target);
        var choose = new Button
        {
            Name = $"Target{target}",
            Text = ChoiceRowWording.Target(DecisionPanelCopy.TargetAction(composer, selected), targetName, selected),
            TooltipText = ChoiceRowWording.Tooltip(targetName, request.Details?.GetValueOrDefault(target)),
            Alignment = HorizontalAlignment.Left,
            ToggleMode = true,
            ButtonPressed = selected,
            Disabled = submitting,
        };
        choose.AccessibilityDescription = choose.TooltipText;
        panel.StyleButton(
            choose,
            choose.ButtonPressed
                ? InteractiveVisualState.Selected
                : InteractiveVisualState.Legal);
        choose.Pressed += () => ToggleOrdinaryTarget(target);
        panel.BindAnchors(choose, target);
        if (detail is null) AddContent(choose);
        else AddContent(WithDetail(choose, target, detail));
    }

    // A detail that tells this target from the others stands beside its row as a chip, whole on hover.
    private HBoxContainer WithDetail(Button choose, int target, string detail)
    {
        var row = new HBoxContainer { Name = $"Target{target}Row", ThemeTypeVariation = GodotThemeVariations.CompactRow };
        choose.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(choose);
        HFlowContainer chip = DraftFactsRow.Create(panel.ReadingScale);
        chip.Name = $"Target{target}Detail";
        chip.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        DraftFactsRow.Fill(chip, [new DraftPart(detail)]);
        row.AddChild(chip);
        return row;
    }

    private void ToggleOrdinaryTarget(int target)
    {
        if (!operations.TryToggleTarget(target)) return;
        if (MulliganPrompt.IsOpening(composer!.Prompt)) panel.RefreshMulliganTargets(composer, target);
        else
        {
            panel.NotifyAnchorFocused([target]);
            panel.Rebuild();
        }
    }

    internal void AddRepeatedTarget(TargetRequest request, int target)
    {
        int count = composer!.Targets.Count(chosen => chosen == target);
        int limit = request.MaximumOccurrences?.GetValueOrDefault(target) ?? request.Max;
        var row = new HBoxContainer { ThemeTypeVariation = GodotThemeVariations.CompactRow };
        row.AddChild(Step(target, "Remove", "−", count > 0, InteractiveVisualState.Resting, () =>
        {
            if (operations.TryRemoveTarget(target)) panel.Rebuild();
        }));
        row.AddChild(RepeatedCount(request, target, count, limit));
        row.AddChild(Step(target, "Add", "+", operations.CanAddTarget(target), InteractiveVisualState.Legal, () =>
        {
            if (!operations.TryAddTarget(target)) return;
            panel.NotifyAnchorFocused([target]);
            panel.Rebuild();
        }));
        AddContent(row);
    }

    // The count as pips against the limit, then a detail that tells this target from the others.
    private HFlowContainer RepeatedCount(TargetRequest request, int target, int count, int limit)
    {
        string name = PromptPresentation.Describe(target, world!);
        HFlowContainer tracker = DraftFactsRow.Create(panel.ReadingScale);
        tracker.Name = $"Target{target}Count";
        tracker.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var parts = new List<DraftPart> { ChoiceRowWording.Repeated(name, request.Details?.GetValueOrDefault(target), count, limit) };
        if (ChoiceRowWording.Distinguishing(request, target, name) is { } detail) parts.Add(new DraftPart(detail));
        DraftFactsRow.Fill(tracker, parts);
        return tracker;
    }

    private Button Step(int target, string operation, string text, bool available, InteractiveVisualState state, Action pressed)
    {
        var step = new Button { Name = $"Target{target}{operation}", Text = text, Disabled = submitting || !available };
        panel.StyleButton(step, step.Disabled ? InteractiveVisualState.Unavailable : state, compact: true);
        step.Pressed += pressed;
        panel.BindAnchors(step, target);
        return step;
    }
}
