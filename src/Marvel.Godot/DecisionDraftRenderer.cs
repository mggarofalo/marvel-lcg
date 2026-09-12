using Godot;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders target selection, costs, and submission for one decision draft.</summary>
internal sealed class DecisionDraftRenderer
{
    private readonly DecisionPanel panel;
    private readonly DecisionComposer composer;
    private readonly WorldDescriptor world;
    private readonly bool submitting;

    internal DecisionDraftRenderer(
        DecisionPanel panel,
        DecisionComposer composer,
        WorldDescriptor world,
        bool submitting)
    {
        this.panel = panel;
        this.composer = composer;
        this.world = world;
        this.submitting = submitting;
    }
    internal void AddTargets(Affordance selected, TargetSelectionProgress progress)
    {
        TargetRequest? request = selected.Targets;
        if (request is null)
        {
            panel.AddContent(DecisionPanel.Text("No target selection", GodotThemeVariations.MutedText));
            return;
        }

        if (composer!.UsesAutomaticTargetSelection)
        {
            Label automatic = DecisionPanel.Text(
                "TARGET  ·  " + string.Join(" → ", composer.Targets.Select(id =>
                    PromptPresentation.Describe(id, world!))) + "  ·  automatic",
                GodotThemeVariations.StatusText,
                wrap: true);
            automatic.Name = "AutomaticTargets";
            panel.BindAnchors(automatic, [.. composer.Targets]);
            panel.AddContent(automatic);
            return;
        }

        string badge = request.IsSearch ? "SEARCH RESULTS" : "TARGETS";
        panel.AddContent(DecisionPanel.Text($"{badge}  ·  " + panel.TargetProgressText(progress),
            GodotThemeVariations.Caption, wrap: true));
        AddTargetInstructions(request);

        if (request.IsGrouped)
        {
            AddGroupedTargets(request);
            return;
        }

        AddLegalTargets(request);
        AddTargetOrder();
    }

    private void AddTargetInstructions(TargetRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Rule))
        {
            panel.AddContent(DecisionPanel.Text(request.Rule, GodotThemeVariations.Caption));
        }

        if (request.MustIncludeTraits is { Count: > 0 })
        {
            panel.AddContent(DecisionPanel.Text(
                $"MUST INCLUDE  ·  {string.Join(", ", request.MustIncludeTraits)}",
                GodotThemeVariations.Caption, wrap: true));
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
                Text = (composer.Targets.SequenceEqual(group)
                        ? "✓ SELECTED  ·  "
                        : "◇ LEGAL  ·  ")
                    + $"Group {index + 1}  ·  "
                    + string.Join(" → ", group.Select(id => PromptPresentation.Describe(id, world))),
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
                composer.SelectTargets(group);
                panel.Rebuild();
            };
            panel.BindAnchors(choose, [.. group]);
            panel.AddContent(choose);
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
        if (composer.Targets.Count > 0)
        {
            panel.AddContent(DecisionPanel.Text(
                "ORDER  ·  " + string.Join(" → ", composer.Targets.Select(id =>
                    PromptPresentation.Describe(id, world))),
                GodotThemeVariations.Eyebrow, wrap: true));
        }
    }

    internal void AddOrdinaryTarget(int target)
    {
        string targetName = PromptPresentation.Describe(target, world!);
        string detail = composer!.Selected?.Targets?.Details?.GetValueOrDefault(target) is { } text
            ? $"  ·  {text}"
            : string.Empty;
        var choose = new Button
        {
            Name = $"Target{target}",
            Text = composer!.Targets.Contains(target)
                ? $"✓ {panel.TargetAction(selected: true)}  ·  {targetName}{detail}"
                : $"◇ {panel.TargetAction(selected: false)}  ·  {targetName}{detail}",
            Alignment = HorizontalAlignment.Left,
            ToggleMode = true,
            ButtonPressed = composer!.Targets.Contains(target),
            Disabled = submitting,
        };
        panel.StyleButton(
            choose,
            choose.ButtonPressed
                ? InteractiveVisualState.Selected
                : InteractiveVisualState.Legal);
        choose.Pressed += () =>
        {
            if (composer.Targets.Contains(target))
            {
                composer.RemoveTarget(target);
            }
            else
            {
                composer.AddTarget(target);
            }
            panel.NotifyAnchorFocused([target]);
            panel.Rebuild();
        };
        panel.BindAnchors(choose, target);
        panel.AddContent(choose);
    }

    internal void AddRepeatedTarget(TargetRequest request, int target)
    {
        int count = composer!.Targets.Count(chosen => chosen == target);
        int limit = request.MaximumOccurrences?.GetValueOrDefault(target) ?? request.Max;
        var row = new HBoxContainer
        {
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
        };
        var remove = new Button
        {
            Name = $"Target{target}Remove",
            Text = "−",
            Disabled = submitting || count == 0,
        };
        panel.StyleButton(
            remove,
            remove.Disabled
                ? InteractiveVisualState.Unavailable
                : InteractiveVisualState.Resting,
            compact: true);
        remove.Pressed += () =>
        {
            composer.RemoveTarget(target);
            panel.Rebuild();
        };
        panel.BindAnchors(remove, target);
        row.AddChild(remove);
        var label = DecisionPanel.Text(
            $"{count}  ·  {PromptPresentation.Describe(target, world!)}  ·  max {limit}",
            GodotThemeVariations.Body, wrap: true);
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(label);
        var add = new Button
        {
            Name = $"Target{target}Add",
            Text = "+",
            Disabled = submitting || count >= limit || composer.Targets.Count >= request.Max,
        };
        panel.StyleButton(
            add,
            add.Disabled
                ? InteractiveVisualState.Unavailable
                : InteractiveVisualState.Legal,
            compact: true);
        add.Pressed += () =>
        {
            composer.AddTarget(target);
            panel.NotifyAnchorFocused([target]);
            panel.Rebuild();
        };
        panel.BindAnchors(add, target);
        row.AddChild(add);
        panel.AddContent(row);
    }
}
