using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Builds explicit card controls from the authorized prompt projection.</summary>
internal static class BoardInteractionControlProjection
{
    internal static IReadOnlyList<CardInteractionControlDescriptor> From(
        DecisionComposer? composer, PromptPresentation? prompt)
    {
        if (composer is null || prompt is null)
        {
            return [];
        }

        var controls = new List<CardInteractionControlDescriptor>();
        AddActionControls(controls, prompt);
        AddGeneratorControls(controls, composer);
        return controls;
    }

    private static void AddActionControls(
        List<CardInteractionControlDescriptor> controls, PromptPresentation prompt)
    {
        foreach (IGrouping<int, AffordancePresentation> actions in prompt.Affordances
                     .Where(affordance => affordance.Source?.CardId is not null
                         && affordance.Illegal is null)
                     .GroupBy(affordance => affordance.Source!.CardId!.Value)
                     .OrderBy(group => group.Key))
        {
            controls.Add(new CardInteractionControlDescriptor(
                actions.Key,
                CardInteractionIntent.Action,
                actions.Count() == 1 ? "◇ ACTION" : $"◇ CHOOSE ACTION ({actions.Count()})",
                CardInteractionCue.OfferedAction));
        }
    }

    private static void AddGeneratorControls(
        List<CardInteractionControlDescriptor> controls, DecisionComposer composer)
    {
        if (composer.Selected is not { } selected || composer.SelectedCost < 0
            || composer.SelectedCost >= selected.CostOptions.Count)
        {
            return;
        }

        foreach (int generator in selected.CostOptions[composer.SelectedCost].Generators
                     .Select(source => source.Effect).Distinct().OrderBy(id => id))
        {
            bool selectedGenerator = composer.Resources.Contains(generator);
            controls.Add(new CardInteractionControlDescriptor(
                generator,
                CardInteractionIntent.Generator,
                selectedGenerator ? "✓ RESOURCE" : "◇ RESOURCE",
                selectedGenerator
                    ? CardInteractionCue.SelectedGenerator
                    : CardInteractionCue.LegalGenerator));
        }
    }
}
