using Godot;
using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>The bounded body and persistent commit bar for one decision dock.</summary>
internal sealed class DecisionPanelLayout
{
    private DecisionPanelLayout(Container content, VBoxContainer commit)
    {
        Content = content;
        Commit = commit;
    }

    internal Container Content { get; }

    internal VBoxContainer Commit { get; }

    internal static DecisionPanelLayout Build(
        DecisionPanel panel,
        PromptPresentation prompt,
        DecisionComposer composer,
        bool submitting,
        Action changeAction)
    {
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        bool browsing = composer.Selected is null;
        if (!browsing)
        {
            AffordancePresentation selected = prompt.Affordances.Single(view =>
                view.Id == composer.Selected!.Id);
            var change = new Button
            {
                Name = "ChangeAction",
                Text = "Change action · " + DecisionCopy.ActionSummary(selected),
                TooltipText = "Return to the complete list of actions offered by this prompt.",
                Disabled = submitting,
            };
            panel.StyleButton(change, submitting
                ? InteractiveVisualState.Unavailable
                : InteractiveVisualState.Resting);
            change.Pressed += changeAction;
            panel.AddChild(change);
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
            VerticalScrollMode = browsing && prompt.Affordances.Count <= 12
                ? ScrollContainer.ScrollMode.Disabled
                : ScrollContainer.ScrollMode.Auto,
        };
        Container content = browsing
            ? new HFlowContainer
            {
                Name = "DecisionBody",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                ThemeTypeVariation = GodotThemeVariations.CompactRow,
            }
            : new VBoxContainer
            {
                Name = "DecisionBody",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                ThemeTypeVariation = GodotThemeVariations.TightStack,
            };
        scroll.AddChild(content);
        panel.AddChild(scroll);

        var commit = new VBoxContainer
        {
            Name = "CommitBar",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        panel.AddChild(new HSeparator());
        panel.AddChild(commit);
        return new DecisionPanelLayout(content, commit);
    }
}
