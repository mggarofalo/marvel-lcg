using Godot;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders payment choices and submission for one decision draft.</summary>
internal sealed class DecisionPaymentRenderer
{
    private readonly DecisionPanel panel;
    private readonly DecisionComposer composer;
    private readonly WorldDescriptor world;
    private readonly bool submitting;

    internal DecisionPaymentRenderer(
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
    internal void AddCosts(Affordance selected)
    {
        if (selected.CostOptions.Count == 0)
        {
            panel.AddContent(DecisionPanel.Text("PAYMENT  ·  FREE  ·  READY", GodotThemeVariations.StatusText));
            return;
        }

        panel.AddContent(DecisionPanel.Text("COST", GodotThemeVariations.Caption));
        AddCostOptions(selected);
        if (composer.SelectedCost < 0)
        {
            AddPendingCostChoice();
            return;
        }

        CostOption selectedCost = selected.CostOptions[composer.SelectedCost];
        AddVariables(selectedCost);
        AddGenerators(selectedCost);
        AddResourceAssignments(selectedCost);
        AddComponents(selectedCost);
        AddPaymentProgress();
    }

    private void AddCostOptions(Affordance selected)
    {
        for (int index = 0; index < selected.CostOptions.Count; index++)
        {
            int costIndex = index;
            CostOption cost = selected.CostOptions[index];
            bool targetMatches = composer!.CostApplies(cost);
            bool unavailable = submitting || !targetMatches;
            bool isSelected = composer.SelectedCost == index;
            var choose = new Button
            {
                Name = $"Cost{costIndex}",
                Text = unavailable
                    ? $"— UNAVAILABLE  ·  {panel.CostLabel(cost)}"
                    : isSelected
                    ? $"✓ SELECTED  ·  {panel.CostLabel(cost)}"
                    : $"◇ CHOOSE  ·  {panel.CostLabel(cost)}",
                Alignment = HorizontalAlignment.Left,
                ToggleMode = true,
                ButtonPressed = isSelected,
                Disabled = unavailable,
            };
            panel.StyleButton(
                choose,
                choose.Disabled
                    ? InteractiveVisualState.Unavailable
                    : choose.ButtonPressed
                        ? InteractiveVisualState.Selected
                        : InteractiveVisualState.Legal);
            choose.Pressed += () =>
            {
                composer.SelectCost(costIndex);
                foreach (VariableRequest variable in cost.VariableRequests)
                {
                    composer.Define(variable.Name, variable.Min);
                }
                panel.Rebuild();
            };
            if (cost.Target != 0)
            {
                panel.BindAnchors(choose, cost.Target);
            }
            panel.AddContent(choose);
        }
    }

    private void AddPendingCostChoice()
    {
        PaymentProgress pending = composer.Progress().Payment;
        panel.AddContent(DecisionPanel.Text(
            $"PAYMENT  ·  CHOOSE 1 OF {pending.CostOptions} COSTS",
            GodotThemeVariations.Caption));
    }

    private void AddVariables(CostOption cost)
    {
        foreach (VariableRequest variable in cost.VariableRequests)
        {
            if (!composer.Values.ContainsKey(variable.Name))
            {
                composer.Define(variable.Name, variable.Min);
            }

            var row = new HBoxContainer();
            var name = DecisionPanel.Text(
                $"{variable.Name}  ·  {variable.Min}–{variable.Max}",
                GodotThemeVariations.Body);
            name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(name);
            var value = new SpinBox
            {
                Name = $"Variable{DecisionPanel.NodeKey(variable.Name)}",
                CustomMinimumSize = new Vector2(
                    panel.ControlMetrics.MinimumButtonWidth,
                    panel.ControlMetrics.MinimumHeight),
                MinValue = variable.Min,
                MaxValue = variable.Max,
                Step = 1,
                Value = composer.Values[variable.Name],
                AllowGreater = false,
                AllowLesser = false,
                Editable = !submitting,
            };
            value.ValueChanged += chosen =>
            {
                composer.Define(variable.Name, checked((long)chosen));
                panel.Rebuild();
            };
            row.AddChild(value);
            panel.AddContent(row);
        }
    }

    private void AddGenerators(CostOption cost)
    {
        foreach (ResourceSource source in cost.Generators)
        {
            var choose = new Button
            {
                Name = $"Resource{source.Effect}",
                Text = (composer.Resources.Contains(source.Effect)
                        ? "✓ SELECTED  ·  "
                        : "◇ RESOURCE  ·  ")
                    + $"{PromptPresentation.Describe(source.Effect, world!)}"
                    + $"  ·  {source.Generates}",
                Alignment = HorizontalAlignment.Left,
                ToggleMode = true,
                ButtonPressed = composer.Resources.Contains(source.Effect),
                Disabled = submitting,
            };
            panel.StyleButton(
                choose,
                choose.ButtonPressed
                    ? InteractiveVisualState.Selected
                    : InteractiveVisualState.Legal);
            choose.Pressed += () =>
            {
                composer.ToggleResource(source.Effect);
                panel.Rebuild();
            };
            panel.BindAnchors(choose, source.Effect);
            panel.AddContent(choose);
        }
    }

    private void AddComponents(CostOption cost)
    {
        if (cost.ResourceCosts.Count > 1)
        {
            panel.AddContent(DecisionPanel.Text(
                "COMPONENTS  ·  " + string.Join(" + ",
                    cost.ResourceCosts.Select((component, index) =>
                        $"{index + 1}:{component.Cost}")),
                GodotThemeVariations.Caption, wrap: true));
        }
    }

    private void AddPaymentProgress()
    {
        PaymentProgress progress = composer.Progress().Payment;
        panel.AddContent(DecisionPanel.Text(
            $"PAYMENT  ·  {progress.SelectedGenerators} GENERATORS"
            + $"  ·  {progress.AssignedIcons}/{progress.GeneratedIcons} ICONS"
            + (progress.ExcessIcons > 0
                ? $"  ·  {progress.ExcessIcons} EXCESS"
                : string.Empty)
            + (progress.RequestedVariables > 0
                ? $"  ·  {progress.DefinedVariables}/{progress.RequestedVariables} VALUES"
                : string.Empty)
            + (progress.IsSatisfied ? "  ·  READY" : "  ·  INCOMPLETE"),
            progress.ExcessIcons > 0
                ? GodotThemeVariations.DangerText
                : progress.IsSatisfied
                ? GodotThemeVariations.StatusText
                : GodotThemeVariations.Caption,
            wrap: true));
    }

    internal void AddResourceAssignments(CostOption cost)
    {
        if (composer!.UsesAutomaticResourceAllocation)
        {
            foreach (ResourceSource source in cost.Generators.Where(generator =>
                         composer.Resources.Contains(generator.Effect)))
            {
                int assigned = composer.Assignments.Count(assignment =>
                    assignment.Source == source.Effect);
                panel.AddContent(DecisionPanel.Text(
                    $"{PromptPresentation.Describe(source.Effect, world!)}"
                    + $"  ·  PRINTED {string.Join(" + ", source.Generates.Select(DecisionPanel.ResourceName))}"
                    + $"  ·  {assigned} APPLIED"
                    + (source.Generates.Length > assigned
                        ? $"  ·  {source.Generates.Length - assigned} EXCESS"
                        : string.Empty),
                    GodotThemeVariations.StatusText,
                    wrap: true));
            }

            return;
        }

        foreach (ResourceSource source in cost.Generators.Where(generator =>
                     composer.Resources.Contains(generator.Effect)))
        {
            for (int icon = 0; icon < source.Generates.Length; icon++)
            {
                int iconIndex = icon;
                char printed = source.Generates[icon];
                var choices = new List<AllocationChoice>
                {
                    new(Cost: null, PaidAs: null, "Unused / excess"),
                };
                for (int component = 0; component < cost.ResourceCosts.Count; component++)
                {
                    if (printed == Resources.Wild)
                    {
                        choices.AddRange(Resources.Types.Select(declared =>
                            new AllocationChoice(
                                component,
                                declared,
                                $"Cost {component + 1} as {DecisionPanel.ResourceName(declared)}")));
                    }
                    else
                    {
                        choices.Add(new AllocationChoice(
                            component,
                            printed,
                            $"Cost {component + 1} as {DecisionPanel.ResourceName(printed)}"));
                    }
                }

                var row = new HBoxContainer();
                var label = DecisionPanel.Text(
                    $"Icon {icon + 1} · {DecisionPanel.ResourceName(printed)}",
                    GodotThemeVariations.Body);
                label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                row.AddChild(label);
                var allocation = new OptionButton
                {
                    Name = $"Allocation{source.Effect}_{iconIndex}",
                    CustomMinimumSize = new Vector2(
                        Math.Max(170, panel.ControlMetrics.MinimumButtonWidth),
                        panel.ControlMetrics.MinimumHeight),
                    Disabled = submitting,
                };
                foreach (AllocationChoice choice in choices)
                {
                    allocation.AddItem(choice.Label);
                }

                ResourceIconAssignment current = composer!.Assignments.FirstOrDefault(
                    assignment => assignment.Source == source.Effect
                        && assignment.Icon == iconIndex);
                int currentIndex = composer.Assignments.Any(assignment =>
                        assignment.Source == source.Effect && assignment.Icon == iconIndex)
                    ? choices.FindIndex(choice =>
                        choice.Cost == current.Cost && choice.PaidAs == current.PaidAs)
                    : 0;
                allocation.Select(Math.Max(0, currentIndex));
                allocation.ItemSelected += selected =>
                {
                    AllocationChoice choice = choices[(int)selected];
                    composer.AssignResource(
                        source.Effect, iconIndex, choice.Cost, choice.PaidAs);
                    panel.Rebuild();
                };
                panel.BindAnchors(allocation, source.Effect);
                row.AddChild(allocation);
                panel.AddContent(row);
            }
        }
    }

    internal void AddSubmit(DecisionProgressPresentation progress)
    {
        if (!progress.IsReady && progress.Error is not null)
        {
            Label validation = DecisionPanel.Text(
                $"! {progress.Error}",
                GodotThemeVariations.DangerText,
                wrap: true);
            validation.Name = "ValidationError";
            panel.AddCommit(validation);
        }

        if (progress.Payment.ExcessIcons > 0)
        {
            panel.AddCommit(DecisionPanel.Text(
                $"! {DecisionCopy.OverpaymentWarning(progress.Payment)}",
                GodotThemeVariations.DangerText,
                wrap: true));
        }

        string action = DecisionCopy.WithPaymentConsequence(
            panel.SubmitAction(), progress.Payment);
        var submit = new Button
        {
            Name = "Submit",
            Text = submitting
                ? "— UNAVAILABLE  ·  Waiting for engine…"
                : progress.IsReady
                    ? action
                    : $"— Unavailable  ·  {action}",
            Disabled = submitting || !progress.IsReady,
        };
        panel.StyleButton(
            submit,
            submit.Disabled
                ? InteractiveVisualState.Unavailable
                : InteractiveVisualState.Danger);
        submit.Pressed += () =>
        {
            if (composer!.TryBuild(out EngineDecision? decision, out _))
            {
                panel.NotifySubmitted(decision!);
            }
        };
        panel.AddCommit(submit);
    }
    private readonly record struct AllocationChoice(
        int? Cost,
        char? PaidAs,
        string Label);
}
