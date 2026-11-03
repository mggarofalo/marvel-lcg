using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Describes editable choices using the current engine-authorized draft progress.</summary>
internal static class TableDraftSummary
{
    /// <summary>The draft's summary; an action description equal to <paramref name="shownInstruction"/> is left out.</summary>
    internal static string? From(
        DecisionComposer? composer, PromptPresentation? prompt, WorldDescriptor? world = null,
        bool compact = false, string? shownInstruction = null) =>
        Parts(composer, prompt, world, compact, shownInstruction) is { } parts
            ? string.Join("  ·  ", parts.Select(part => part.Text)) : null;

    /// <summary>
    /// The draft as parts, in reading order: each with its words, and its short label and pips when a tracker
    /// shows it on the table. Null when nothing is selected.
    /// </summary>
    internal static IReadOnlyList<DraftPart>? Parts(
        DecisionComposer? composer, PromptPresentation? prompt, WorldDescriptor? world = null,
        bool compact = false, string? shownInstruction = null)
    {
        if (composer?.Selected is not { } selected)
        {
            return null;
        }

        DecisionProgressPresentation progress = composer.Progress();
        AffordancePresentation? visible = Visible(prompt, selected.Id, shownInstruction);
        var state = new List<DraftPart>();
        AddSelectedDefender(state, composer, visible, compact);
        if (selected.Verb is not (Game.ResolveMulligans or Game.EndPhaseVerb))
        {
            state.Add(new(visible is null ? selected.Label : compact
                ? DecisionCopy.CompactActionSummary(visible) : DecisionCopy.ActionSummary(visible)));
        }
        AddTargets(state, progress.Targets, selected.Verb);
        AddSelectedObjects(state, composer, world);
        var payment = new List<string>();
        TablePaymentSummary.Add(payment, progress.Payment, compact);
        state.AddRange(payment.Select((text, index) => index == 0 && progress.Payment.CostState == CostSelectionState.Selected
            ? PaymentPart(text, progress.Payment) : new DraftPart(text)));
        AddOutstandingChoice(state, progress, compact);
        return [.. state.Where(value => !string.IsNullOrWhiteSpace(value.Text))];
    }

    private static DraftPart PaymentPart(string text, PaymentProgress payment) =>
        PaymentProgressTracker.Pips(payment) is { } pips
            ? new(text, DecisionWording.DraftPaid, pips.Filled, pips.Total)
            : new(text);

    private static AffordancePresentation? Visible(PromptPresentation? prompt, int id, string? shownInstruction)
    {
        AffordancePresentation? visible = prompt?.Affordances.FirstOrDefault(affordance => affordance.Id == id);
        return visible?.Description is { Length: > 0 } description
            && string.Equals(description.Trim(), shownInstruction, StringComparison.Ordinal)
                ? visible with { Description = null } : visible;
    }

    private static void AddSelectedDefender(
        List<DraftPart> state, DecisionComposer composer, AffordancePresentation? visible, bool compact)
    {
        if (compact && composer.Prompt.PublicKind == PublicDecisionKind.Defense
            && visible?.SourceState is { Length: > 0 } sourceState)
        {
            // A selected defender can be outside the collection's inspected card.
            state.Add(new(string.Join(" · ", new[] { visible.SourceName, sourceState }
                .Where(value => !string.IsNullOrWhiteSpace(value)))));
        }
    }

    private static void AddOutstandingChoice(
        List<DraftPart> state, DecisionProgressPresentation progress, bool compact)
    {
        if (!progress.IsReady && !string.IsNullOrWhiteSpace(progress.Error)
            && !(compact && TablePaymentSummary.ExplainsOutstandingChoice(progress)))
        {
            state.Add(new(progress.Error));
        }
    }

    private static void AddSelectedObjects(
        List<DraftPart> state, DecisionComposer composer, WorldDescriptor? world)
    {
        foreach (int target in composer.Targets)
        {
            string? detail = composer.Selected?.Targets?.Details?.GetValueOrDefault(target);
            string? name = world is null ? null : PromptPresentation.Describe(target, world);
            string? description = SelectedObjectDescription(name, detail);
            if (description is not null) state.Add(new(description));
        }
    }

    private static string? SelectedObjectDescription(string? name, string? detail) =>
        string.IsNullOrWhiteSpace(detail) ? name
            : name is null ? detail : $"{name}: {detail}";

    private static void AddTargets(List<DraftPart> state, TargetSelectionProgress targets, string verb)
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
            bool mulligan = verb == Game.ResolveMulligans;
            string purpose = mulligan ? "replacement" : "discard";
            string staged = $"{targets.Selected} card{(targets.Selected == 1 ? string.Empty : "s")} staged for {purpose} ({bounds}).";
            state.Add(new(mulligan ? $"{staged}  ·  Replacements are drawn after you confirm." : staged,
                mulligan ? DecisionWording.DraftRedraw : DecisionWording.DraftDiscard, targets.Selected, targets.Maximum));
            return;
        }
        state.Add(targets.Mode == TargetSelectionMode.Grouped
            ? new($"{targets.Selected} group selected (1 required).", DecisionWording.DraftTargets, targets.Selected, 1)
            : new($"{targets.Selected} selected ({bounds}).", DecisionWording.DraftTargets, targets.Selected, targets.Maximum));
    }

}
