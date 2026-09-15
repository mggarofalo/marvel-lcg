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
        var scroll = new ScrollContainer
        {
            Name = "DecisionBodyScroll",
            CustomMinimumSize = new Vector2(0, panel.ControlMetrics.MinimumPointerTarget),
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
        AddActionSummary(body, selected, composer);
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

    private static void AddActionSummary(
        VBoxContainer body,
        AffordancePresentation? selected,
        DecisionComposer composer)
    {
        if (selected is null)
        {
            return;
        }

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
        body.AddChild(summary);
        if (!MulliganPrompt.IsOpening(composer.Prompt))
        {
            body.AddChild(new HSeparator());
        }
    }

    internal static void AddAffordances(DecisionPanel panel, PromptPresentation prompt, int generation)
    {
        if (MulliganPrompt.IsOpening(panel.composer?.Prompt))
        {
            return;
        }
        var basic = new HashSet<int>();
        AffordancePresentation[] plays = [.. prompt.Affordances
            .Where(view => view.Verb == CardPlay.Verb)];
        foreach (AffordancePresentation view in prompt.Affordances.Except(plays))
        {
            if (view.Verb is "Attack" or "Thwart" or "Recover" && basic.Add(view.AnchorId))
                panel.AddContent(DecisionPanel.Text($"BASIC ACTIONS  ·  {view.Anchor}", GodotThemeVariations.Eyebrow, wrap: true));
            Add(panel, view, generation);
        }
        AddCardPlayMenu(panel, plays, generation);
    }

    private static void AddCardPlayMenu(
        DecisionPanel panel,
        AffordancePresentation[] plays,
        int generation)
    {
        if (plays.Length == 0)
        {
            return;
        }

        var choices = new VBoxContainer
        {
            Name = "CardPlayChoices",
            Visible = false,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        var disclosure = new Button
        {
            Name = "CardPlayMenu",
            Text = $"▸ Play a card  ·  {plays.Length}",
            Alignment = HorizontalAlignment.Left,
            ToggleMode = true,
            TooltipText = "Show keyboard-accessible card-play choices. Cards can also be dragged from hand.",
        };
        panel.StyleButton(disclosure, InteractiveVisualState.Resting);
        disclosure.Pressed += () =>
        {
            choices.Visible = disclosure.ButtonPressed;
            disclosure.Text = $"{(disclosure.ButtonPressed ? "▾" : "▸")} Play a card  ·  {plays.Length}";
        };
        panel.AddContent(disclosure);
        panel.AddContent(choices);
        foreach (AffordancePresentation play in plays)
        {
            Add(panel, play, generation, choices);
        }
    }

    private static void Add(
        DecisionPanel panel,
        AffordancePresentation view,
        int generation,
        Container? destination = null)
    {
        Affordance option = panel.composer!.Prompt.Affordances.Single(candidate => candidate.Id == view.Id);
        bool unavailable = panel.submitting || !option.IsLegal;
        bool selected = panel.composer.Selected?.Id == option.Id;
        bool resolving = panel.submitting && selected;
        var choose = new Button { Name = $"Affordance{option.Id}", Text = Text(DecisionCopy.Choice(view), unavailable, selected, resolving), Alignment = HorizontalAlignment.Left, Disabled = unavailable, ToggleMode = true, ButtonPressed = selected, TooltipText = option.Illegal ?? $"Anchor {option.AnchorId}, player {option.AnchorPlayer}" };
        panel.StyleButton(choose, resolving ? InteractiveVisualState.Selected : unavailable ? InteractiveVisualState.Unavailable : selected ? InteractiveVisualState.Selected : InteractiveVisualState.Resting);
        choose.Pressed += () => panel.SelectAffordance(option.Id, generation);
        panel.BindAnchors(choose, option.AnchorId);
        Add(destination, panel, choose);
        if (option.Illegal is not null)
            Add(destination, panel, DecisionPanel.Text($"! {option.Illegal}", GodotThemeVariations.DangerText, wrap: true));
    }

    private static void Add(Container? destination, DecisionPanel panel, Control control)
    {
        if (destination is null) panel.AddContent(control);
        else destination.AddChild(control);
    }

    private static string Text(string action, bool unavailable, bool selected, bool resolving) => resolving ? $"✓ {action}  ·  resolving" : unavailable ? $"— Unavailable  ·  {action}" : selected ? $"✓ {action}" : action;
}
