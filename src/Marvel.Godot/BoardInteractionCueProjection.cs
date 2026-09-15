using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Projects only the authorized prompt and draft into card interaction cues.</summary>
internal static class BoardInteractionCueProjection
{
    internal static IReadOnlyDictionary<int, CardInteractionCue> From(
        DecisionComposer? composer, PromptPresentation? prompt)
    {
        var cues = new Dictionary<int, CardInteractionCue>();
        if (composer is null || prompt is null)
        {
            return cues;
        }

        AddActionCues(cues, prompt);
        AddSelectedCues(cues, composer, prompt);
        return cues;
    }

    private static void AddActionCues(
        Dictionary<int, CardInteractionCue> cues, PromptPresentation prompt)
    {
        foreach (AffordancePresentation affordance in prompt.Affordances.Where(
                     affordance => affordance.Source?.CardId is not null))
        {
            Add(cues, affordance.Source!.CardId!.Value, CueFor(affordance));
        }
    }

    private static void AddSelectedCues(
        Dictionary<int, CardInteractionCue> cues,
        DecisionComposer composer,
        PromptPresentation prompt)
    {
        AffordancePresentation? selected = composer.Selected is null ? null : prompt.Affordances
            .SingleOrDefault(affordance => affordance.Id == composer.Selected.Id);
        if (selected is null)
        {
            return;
        }
        AddSelectedDestructiveCue(cues, selected);
        Add(cues, selected.TargetRequest?.Legal ?? [], CardInteractionCue.LegalTarget);
        Add(cues, composer.Targets, CardInteractionCue.SelectedTarget);
        if (composer.SelectedCost >= 0 && composer.SelectedCost < selected.CostOptions.Count)
        {
            Add(cues, selected.CostOptions[composer.SelectedCost].Generators
                .Select(source => source.Effect), CardInteractionCue.LegalGenerator);
        }
        Add(cues, composer.Resources, CardInteractionCue.SelectedGenerator);
    }

    private static bool IsDestructive(AffordancePresentation affordance) =>
        affordance.Description?.StartsWith("Discard ", StringComparison.Ordinal) == true;

    private static CardInteractionCue CueFor(AffordancePresentation affordance) =>
        affordance.Illegal is not null
            ? CardInteractionCue.Unavailable
            : IsDestructive(affordance)
                ? CardInteractionCue.DestructiveChoice
                : CardInteractionCue.OfferedAction;

    private static void AddSelectedDestructiveCue(
        Dictionary<int, CardInteractionCue> cues, AffordancePresentation selected)
    {
        if (selected.Source?.CardId is { } id && IsDestructive(selected))
        {
            Add(cues, id, CardInteractionCue.SelectedDestructiveChoice);
        }
    }

    private static void Add(
        Dictionary<int, CardInteractionCue> cues, IEnumerable<int> ids, CardInteractionCue cue)
    {
        foreach (int id in ids)
        {
            Add(cues, id, cue);
        }
    }

    private static void Add(
        Dictionary<int, CardInteractionCue> cues, int id, CardInteractionCue cue) =>
        cues[id] = cues.TryGetValue(id, out CardInteractionCue current) ? current | cue : cue;
}
