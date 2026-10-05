using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Builds the decision-specific labels without owning its controls.</summary>
internal static class DecisionPanelCopy
{
    internal static string TargetProgress(DecisionComposer? composer, TargetSelectionProgress progress) =>
        composer?.Selected?.Verb == "Resolve Mulligans"
            ? $"{progress.Selected} replacements selected"
            : composer?.Selected?.Verb == Game.EndPhaseVerb
                ? progress.Minimum == 0
                    ? $"{progress.Selected} staged for discard · Optional, up to {progress.Maximum}"
                    : $"{progress.Selected} staged for discard · Choose {progress.Minimum}–{progress.Maximum}"
            : progress.Mode switch
            {
                TargetSelectionMode.None => string.Empty,
                TargetSelectionMode.Grouped => $"Choose one complete group · {progress.Selected} selected",
                _ => progress.Minimum == progress.Maximum
                    ? $"Choose {progress.Minimum} targets · {progress.Selected} selected"
                    : $"Choose {progress.Minimum}–{progress.Maximum} targets · {progress.Selected} selected",
            };

    internal static string TargetAction(DecisionComposer composer, bool selected) => composer.Selected?.Verb switch
    {
        "Resolve Mulligans" => "Replace", "End Phase" => "Stage discard",
        "Attack" => "Attack", "Thwart" => "Thwart", _ => selected ? "Selected" : "Select",
    };

    internal static string SubmitAction(DecisionComposer composer, WorldDescriptor world)
    {
        Affordance selected = composer.Selected!;
        if (selected.CommitLabel is { Length: > 0 } commitment) return commitment;
        int count = composer.Targets.Count;
        return selected.Verb switch
        {
            "Resolve Mulligans" when count == 0 => "Keep hand", "Resolve Mulligans" => $"Replace {count} card{(count == 1 ? string.Empty : "s")}",
            "End Phase" when count == 0 => "Keep hand", "End Phase" => $"Discard {count}",
            "Play" => $"Play {PromptPresentation.Describe(selected.AnchorId, world)}",
            "Attack" when count == 1 => $"Attack {PromptPresentation.Describe(composer.Targets[0], world)}",
            "Thwart" when count == 1 => $"Thwart {PromptPresentation.Describe(composer.Targets[0], world)}",
            _ => DecisionCopy.GenericCommit(selected.Verb, selected.DisplayLabel ?? selected.Label, PromptPresentation.Describe(selected.AnchorId, world)),
        };
    }
}
