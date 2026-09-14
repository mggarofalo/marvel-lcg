using Godot;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Builds the current prompt's layout and affordance controls.</summary>
internal static class DecisionPanelPromptRenderer
{
    internal static void CreateLayout(
        DecisionPanel panel, DecisionComposer composer, PromptPresentation prompt)
    {
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        AffordancePresentation? selected = composer.Selected is { } option
            ? prompt.Affordances.Single(view => view.Id == option.Id)
            : null;
        if (selected is not null)
        {
            var summary = new VBoxContainer
            {
                Name = "ActionSummary",
                ThemeTypeVariation = GodotThemeVariations.TightStack,
            };
            summary.AddChild(DecisionPanel.Text("Preparing", GodotThemeVariations.Eyebrow));
            summary.AddChild(DecisionPanel.Text(
                DecisionCopy.ActionSummary(selected),
                selected.Consequence is null
                    ? GodotThemeVariations.StatusText
                    : GodotThemeVariations.DangerText,
                wrap: true));
            panel.AddChild(summary);
            if (!MulliganPrompt.IsOpening(composer.Prompt))
            {
                panel.AddChild(new HSeparator());
            }
        }

        var scroll = new ScrollContainer
        {
            Name = "DecisionBodyScroll",
            CustomMinimumSize = composer.Selected?.CostOptions.Any(cost =>
                cost.Generators.Count > 0) == true
                    ? new Vector2(0, panel.ControlMetrics.MinimumPointerTarget)
                    : Vector2.Zero,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            FollowFocus = true,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
        };
        var body = new VBoxContainer
        {
            Name = "DecisionBody",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        scroll.AddChild(body);
        panel.AddChild(scroll);

        var commit = new VBoxContainer
        {
            Name = "CommitBar",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        panel.AddChild(new HSeparator());
        panel.AddChild(commit);
        panel.InstallLayout(body, commit);
    }

    internal static void AddAffordances(DecisionPanel panel, PromptPresentation prompt, int generation)
    {
        if (MulliganPrompt.IsOpening(panel.composer?.Prompt))
        {
            return;
        }
        var basic = new HashSet<int>();
        foreach (AffordancePresentation view in prompt.Affordances)
        {
            if (view.Verb is "Attack" or "Thwart" or "Recover" && basic.Add(view.AnchorId))
                panel.AddContent(DecisionPanel.Text($"BASIC ACTIONS  ·  {view.Anchor}", GodotThemeVariations.Eyebrow, wrap: true));
            Add(panel, view, generation);
        }
    }

    private static void Add(DecisionPanel panel, AffordancePresentation view, int generation)
    {
        Affordance option = panel.composer!.Prompt.Affordances.Single(candidate => candidate.Id == view.Id);
        bool unavailable = panel.submitting || !option.IsLegal;
        bool selected = panel.composer.Selected?.Id == option.Id;
        bool resolving = panel.submitting && selected;
        var choose = new Button { Name = $"Affordance{option.Id}", Text = Text(DecisionCopy.Choice(view), unavailable, selected, resolving), Alignment = HorizontalAlignment.Left, Disabled = unavailable, ToggleMode = true, ButtonPressed = selected, TooltipText = option.Illegal ?? $"Anchor {option.AnchorId}, player {option.AnchorPlayer}" };
        panel.StyleButton(choose, resolving ? InteractiveVisualState.Selected : unavailable ? InteractiveVisualState.Unavailable : selected ? InteractiveVisualState.Selected : InteractiveVisualState.Resting);
        choose.Pressed += () => panel.SelectAffordance(option.Id, generation);
        panel.BindAnchors(choose, option.AnchorId);
        panel.AddContent(choose);
        if (option.Illegal is not null) panel.AddContent(DecisionPanel.Text($"! {option.Illegal}", GodotThemeVariations.DangerText, wrap: true));
    }

    private static string Text(string action, bool unavailable, bool selected, bool resolving) => resolving ? $"✓ {action}  ·  resolving" : unavailable ? $"— Unavailable  ·  {action}" : selected ? $"✓ {action}" : action;
}
