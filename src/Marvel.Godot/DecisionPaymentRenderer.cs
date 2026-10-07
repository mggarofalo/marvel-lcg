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
    private readonly int generation;
    private readonly TableDraftBinding operations;

    internal DecisionPaymentRenderer(
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
    internal void AddCosts(Affordance selected)
    {
        if (selected.CostOptions.Count == 0)
        {
            return;
        }
        AddCostOptions(selected);
        if (composer.SelectedCost < 0)
        {
            AddPendingCostChoice();
            return;
        }
        CostOption selectedCost = selected.CostOptions[composer.SelectedCost];
        AddVariables(selectedCost);
        if (panel.PaymentModalOpen) new DecisionPaymentSources(panel, composer, generation).Add(selectedCost);
        else AddGenerators(selectedCost);
        new DecisionPaymentResourceAssignmentRenderer(
            panel, composer, world, submitting, generation).Add(selectedCost);
        AddPaymentProgress();
    }
    private void AddCostOptions(Affordance selected)
    {
        if (panel.PaymentModalOpen && selected.CostOptions.Count == 1)
        {
            panel.AddContent(DecisionCostLabel.Create(selected.CostOptions[0], world, showTarget: false));
            return;
        }
        for (int index = 0; index < selected.CostOptions.Count; index++)
            AddCostOption(selected.CostOptions[index], index);
    }

    private void AddCostOption(CostOption cost, int costIndex)
    {
        bool targetMatches = composer!.CostApplies(cost);
        bool unavailable = submitting || !targetMatches;
        bool isSelected = composer.SelectedCost == costIndex;
        var choose = new Button
        {
            Name = $"Cost{costIndex}",
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
        ResourceIconRendering.ButtonContent(choose, DecisionCostLabel.Create(cost, world,
            unavailable ? "— Unavailable · " : isSelected ? "✓ Selected · " : "Choose · "),
            DecisionCostLabel.Accessible(cost));
        choose.Pressed += () =>
        {
            if (operations.TrySelectCost(costIndex))
            {
                panel.Rebuild();
            }
        };
        if (cost.Target != 0)
        {
            panel.BindAnchors(choose, cost.Target);
        }
        panel.AddContent(choose);
    }

    private void AddPendingCostChoice()
    {
        panel.AddContent(DecisionPanel.Text("Choose a cost option to continue.", GodotThemeVariations.Caption));
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
                if (!panel.IsCurrentDraft(composer, generation)) return;
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
            bool available = DecisionResourceEligibility.CanToggle(composer, source.Effect);
            var choose = new Button
            {
                Name = $"Resource{source.Effect}",
                Text = (composer.Resources.Contains(source.Effect)
                        ? "✓ "
                        : "")
                    + $"{PromptPresentation.Describe(source.Effect, world!)}"
                    + (available ? "" : " · Does not pay this cost"),
                Alignment = HorizontalAlignment.Left,
                ToggleMode = true,
                ButtonPressed = composer.Resources.Contains(source.Effect),
                Disabled = submitting || !available,
            };
            ResourceIconRendering.Apply(choose, source.Generates);
            panel.StyleButton(
                choose,
                choose.ButtonPressed
                    ? InteractiveVisualState.Selected
                    : InteractiveVisualState.Legal);
            choose.Pressed += () =>
            {
                if (operations.TryToggleGenerator(source.Effect)) panel.Rebuild();
            };
            panel.BindAnchors(choose, source.Effect);
            panel.AddContent(choose);
        }
    }
    private void AddPaymentProgress()
    {
        PaymentProgress progress = composer.Progress().Payment;
        string summary = $"{progress.GeneratedIcons} resource{(progress.GeneratedIcons == 1 ? "" : "s")} selected";
        if (progress.AssignedIcons != progress.GeneratedIcons)
            summary += $" · {progress.AssignedIcons} assigned to payment";
        if (progress.ExcessIcons > 0) summary += $" · {progress.ExcessIcons} excess";
        Label label = DecisionPanel.Text(summary,
            progress.ExcessIcons > 0 ? GodotThemeVariations.DangerText : GodotThemeVariations.Caption,
            wrap: true);
        if (panel.PaymentModalOpen) panel.AddCommit(label);
        else panel.AddContent(label);
    }

    internal void AddSubmit(DecisionProgressPresentation progress)
    {
        if (!progress.IsReady && progress.Error is not null
            && (!panel.PaymentModalOpen || progress.Payment.IsSatisfied))
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
            DecisionPanelCopy.SubmitAction(composer, world), progress.Payment);
        if (panel.PaymentModalOpen)
            action = $"Pay and play {PromptPresentation.Describe(composer.Selected!.AnchorId, world)}";
        var submit = new Button
        {
            Name = "Submit",
            Text = submitting ? "Playing…" : action,
            Disabled = submitting || !progress.IsReady,
        };
        panel.StyleButton(
            submit,
            submit.Disabled
                ? InteractiveVisualState.Unavailable
                : InteractiveVisualState.Danger);
        submit.Pressed += TrySubmit;
        panel.AddCommit(submit);
    }

    private void TrySubmit()
    {
        if (panel.IsCurrentDraft(composer, generation)
            && composer.TryBuild(out EngineDecision? decision, out _))
        {
            panel.NotifySubmitted(decision!, generation);
        }
    }
}
