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
            ? $"DISCARD AND REDRAW  ·  {progress.Selected} CHOSEN" + (progress.IsSatisfied ? "  ·  READY" : "  ·  INCOMPLETE")
            : progress.Mode switch
            {
                TargetSelectionMode.None => "NO TARGET SELECTION",
                TargetSelectionMode.Grouped => $"GROUP  ·  {progress.Selected}/1 CHOSEN" + (progress.IsSatisfied ? "  ·  COMPLETE" : "  ·  INCOMPLETE"),
                _ => $"TARGETS  ·  {progress.Selected} CHOSEN"
                    + (progress.Minimum == progress.Maximum ? $"  ·  REQUIRED {progress.Minimum}" : $"  ·  REQUIRED {progress.Minimum}–{progress.Maximum}")
                    + (progress.IsSatisfied ? "  ·  COMPLETE" : "  ·  INCOMPLETE"),
            };

    internal static string TargetAction(DecisionComposer composer, bool selected) => composer.Selected?.Verb switch
    {
        "Resolve Mulligans" => "DISCARD AND REDRAW", "End Phase" => "DISCARD",
        "Attack" => "ATTACK", "Thwart" => "THWART", _ => selected ? "CHOSEN" : "TARGET",
    };

    internal static string SubmitAction(DecisionComposer composer, WorldDescriptor world)
    {
        Affordance selected = composer.Selected!;
        int count = composer.Targets.Count;
        return selected.Verb switch
        {
            "Resolve Mulligans" when count == 0 => "Keep hand", "Resolve Mulligans" => $"Discard {count} and redraw",
            "End Phase" when count == 0 => "End player phase", "End Phase" => $"Discard {count} and end player phase",
            "Play" => $"Play {PromptPresentation.Describe(selected.AnchorId, world)}",
            "Attack" when count == 1 => $"Attack {PromptPresentation.Describe(composer.Targets[0], world)}",
            "Thwart" when count == 1 => $"Thwart {PromptPresentation.Describe(composer.Targets[0], world)}",
            _ => DecisionCopy.GenericCommit(selected.Verb, selected.Label, PromptPresentation.Describe(selected.AnchorId, world)),
        };
    }
}
