using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Describes editable choices using the current engine-authorized draft progress.</summary>
internal static class TableDraftSummary
{
    internal static string? From(
        DecisionComposer? composer, PromptPresentation? prompt, WorldDescriptor? world = null,
        bool compact = false)
    {
        if (composer?.Selected is not { } selected)
        {
            return null;
        }

        DecisionProgressPresentation progress = composer.Progress();
        AffordancePresentation? visible = prompt?.Affordances.FirstOrDefault(
            affordance => affordance.Id == selected.Id);
        var state = new List<string>();
        AddSelectedDefender(state, composer, visible, compact);
        if (selected.Verb is not (Game.ResolveMulligans or Game.EndPhaseVerb))
        {
            state.Add(visible is null ? selected.Label : compact
                ? DecisionCopy.CompactActionSummary(visible) : DecisionCopy.ActionSummary(visible));
        }
        AddTargets(state, progress.Targets, selected.Verb);
        AddSelectedObjects(state, composer, world);
        TablePaymentSummary.Add(state, progress.Payment, compact);
        AddOutstandingChoice(state, progress, compact);
        return string.Join("  ·  ", state.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static void AddSelectedDefender(
        List<string> state, DecisionComposer composer, AffordancePresentation? visible, bool compact)
    {
        if (compact && composer.Prompt.PublicKind == PublicDecisionKind.Defense
            && visible?.SourceState is { Length: > 0 } sourceState)
        {
            // A selected defender can be outside the collection's inspected card.
            state.Add(string.Join(" · ", new[] { visible.SourceName, sourceState }
                .Where(value => !string.IsNullOrWhiteSpace(value))));
        }
    }

    private static void AddOutstandingChoice(
        List<string> state, DecisionProgressPresentation progress, bool compact)
    {
        if (!progress.IsReady && !string.IsNullOrWhiteSpace(progress.Error)
            && !(compact && TablePaymentSummary.ExplainsOutstandingChoice(progress)))
        {
            state.Add(progress.Error);
        }
    }

    private static void AddSelectedObjects(
        List<string> state, DecisionComposer composer, WorldDescriptor? world)
    {
        foreach (int target in composer.Targets)
        {
            string? detail = composer.Selected?.Targets?.Details?.GetValueOrDefault(target);
            string? name = world is null ? null : PromptPresentation.Describe(target, world);
            string? description = SelectedObjectDescription(name, detail);
            if (description is not null) state.Add(description);
        }
    }

    private static string? SelectedObjectDescription(string? name, string? detail) =>
        string.IsNullOrWhiteSpace(detail) ? name
            : name is null ? detail : $"{name}: {detail}";

    private static void AddTargets(List<string> state, TargetSelectionProgress targets, string verb)
    {
        if (targets.Mode == TargetSelectionMode.None)
        {
            return;
        }
        string bounds = targets.Minimum == 0
            ? $"up to {targets.Maximum}"
            : targets.Minimum == targets.Maximum
                ? $"{targets.Minimum} required"
                : $"{targets.Minimum}–{targets.Maximum} required";
        if (verb is Game.ResolveMulligans or Game.EndPhaseVerb)
        {
            string purpose = verb == Game.ResolveMulligans ? "replacement" : "discard";
            state.Add($"{targets.Selected} card{(targets.Selected == 1 ? string.Empty : "s")} staged for {purpose} ({bounds}).");
            if (verb == Game.ResolveMulligans)
                state.Add("Replacements are drawn after you confirm.");
            return;
        }
        state.Add(targets.Mode == TargetSelectionMode.Grouped
            ? $"{targets.Selected} group selected (1 required)."
            : $"{targets.Selected} selected ({bounds}).");
    }

}
