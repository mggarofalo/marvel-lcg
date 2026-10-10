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
        var body = new VBoxContainer
        {
            Name = "DecisionBody",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        bool search = panel.CompleteChoicesOpen && DecisionCardChoices.IsChoice(composer.Prompt);
        if (!panel.PaymentModalOpen && !search) AddActionSummary(body, selected, composer);
        if (search)
        {
            panel.LayoutHost.AddChild(body);
        }
        else
        {
            ScrollContainer scroll = CreateScroll(panel);
            scroll.AddChild(body);
            panel.LayoutHost.AddChild(scroll);
        }

        var commit = new VBoxContainer
        {
            Name = "CommitBar",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        panel.LayoutHost.AddChild(new HSeparator());
        panel.LayoutHost.AddChild(commit);
        panel.InstallLayout(body, commit);
    }

    private static ScrollContainer CreateScroll(DecisionPanel panel)
    {
        return new ScrollContainer
        {
            Name = "DecisionBodyScroll",
            CustomMinimumSize = new Vector2(0, panel.PaymentModalOpen
                ? panel.ControlMetrics.MinimumHeight * 2 + 16
                : panel.ControlMetrics.MinimumPointerTarget),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            FollowFocus = true,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
        };
    }

    private static void AddActionSummary(
        VBoxContainer body,
        AffordancePresentation? selected,
        DecisionComposer composer)
    {
        if (selected is null || MulliganPrompt.IsOpening(composer.Prompt))
        {
            return;
        }

        var summary = new VBoxContainer
        {
            Name = "ActionSummary",
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
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
        if (panel.CompleteChoicesOpen && panel.composer?.Selected is not null
            && InitialTableDraft.IsVisibleCardChoice(panel.composer.Prompt)) return;
        if (panel.CompleteChoicesOpen && SearchChoiceGallery.IsChoice(panel.composer?.Prompt))
        {
            panel.CardChoices.AddSearch(prompt, generation);
            return;
        }
        if (MulliganPrompt.IsOpening(panel.composer?.Prompt))
        {
            return;
        }
        AddCategories(panel, prompt, generation);
    }

    private static void AddCategories(DecisionPanel panel, PromptPresentation prompt, int generation)
    {
        foreach (var category in prompt.Affordances.GroupBy(view => CompleteChoiceGroups.Category(view, panel))
            .OrderBy(group => group.Key))
        {
            var section = new VBoxContainer
            {
                Name = $"ChoiceGroup{category.Key}",
                ThemeTypeVariation = GodotThemeVariations.TightStack,
            };
            section.AddChild(DecisionPanel.Text(CompleteChoiceGroups.Headings[category.Key],
                GodotThemeVariations.Body));
            panel.AddContent(section);
            AddSources(panel, category.Where(view => view.Illegal is null), generation, section);
            AffordancePresentation[] unavailable = [.. category.Where(view => view.Illegal is not null)];
            if (unavailable.Length > 0) AddUnavailable(panel, unavailable, generation, section);
        }
    }

    private static void AddSources(DecisionPanel panel, IEnumerable<AffordancePresentation> offers,
        int generation, Container section)
    {
        foreach (var source in offers.GroupBy(CompleteChoiceGroups.SourceKey))
        {
            if (source.Count() > 1)
                section.AddChild(DecisionPanel.Text(source.First().SourceName ?? source.First().Anchor,
                    GodotThemeVariations.Caption, wrap: true));
            foreach (AffordancePresentation view in source) Add(panel, view, generation, section);
        }
    }

    private static void AddUnavailable(DecisionPanel panel, AffordancePresentation[] offers,
        int generation, Container section)
    {
        var details = new VBoxContainer { Visible = false };
        var toggle = new Button
        {
            Text = $"Unavailable ({offers.Length})", ToggleMode = true,
            Alignment = HorizontalAlignment.Left,
        };
        panel.StyleButton(toggle, InteractiveVisualState.Resting, compact: true);
        toggle.Toggled += open => details.Visible = open;
        section.AddChild(toggle);
        section.AddChild(details);
        AddSources(panel, offers, generation, details);
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
        var choose = new Button { Name = $"Affordance{option.Id}", Text = Text(DecisionCopy.Choice(view), unavailable, selected, resolving), Alignment = HorizontalAlignment.Left, Disabled = unavailable, ToggleMode = true, ButtonPressed = selected, TooltipText = option.Illegal ?? DecisionCopy.ActionSummary(view) };
        panel.StyleButton(choose, resolving ? InteractiveVisualState.Selected : unavailable ? InteractiveVisualState.Unavailable : selected ? InteractiveVisualState.Selected : InteractiveVisualState.Resting);
        choose.Pressed += () => panel.SelectAffordance(option.Id, generation);
        panel.BindAnchors(choose, option.AnchorId);
        Add(destination, panel, choose);
        if (option.Illegal is not null)
            Add(destination, panel, DecisionPanel.Text(option.Illegal, GodotThemeVariations.Caption, wrap: true));
    }

    private static void Add(Container? destination, DecisionPanel panel, Control control)
    {
        if (destination is null) panel.AddContent(control);
        else destination.AddChild(control);
    }

    private static string Text(string action, bool unavailable, bool selected, bool resolving) => resolving ? $"✓ {action}  ·  resolving" : unavailable ? $"— Unavailable  ·  {action}" : selected ? $"✓ {action}" : action;
}
