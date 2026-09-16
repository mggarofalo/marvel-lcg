using Marvel.Decisions;
using Marvel.Rules.Prompts;
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
        if (composer.Selected is null)
        {
            AddActionControls(controls, prompt);
        }
        AddCostControls(controls, composer);
        AddTargetControls(controls, composer);
        AddGeneratorControls(controls, composer);
        AddSubmitControl(controls, composer);
        return controls;
    }

    private static void AddCostControls(
        List<CardInteractionControlDescriptor> controls, DecisionComposer composer)
    {
        if (composer.Selected is not { CostOptions.Count: > 1 } selected)
        {
            return;
        }
        for (int index = 0; index < selected.CostOptions.Count; index++)
        {
            string marker = composer.SelectedCost == index ? "✓" : "◇";
            controls.Add(new CardInteractionControlDescriptor(
                selected.AnchorId,
                CardInteractionIntent.Cost,
                $"{marker} COST {index + 1}",
                CardInteractionCue.OfferedAction,
                index));
        }
    }

    private static void AddSubmitControl(
        List<CardInteractionControlDescriptor> controls, DecisionComposer composer)
    {
        if (composer.Selected is not { } selected || !composer.Progress().IsReady)
        {
            return;
        }
        controls.Add(new CardInteractionControlDescriptor(
            selected.AnchorId,
            CardInteractionIntent.Submit,
            "EXECUTE",
            CardInteractionCue.OfferedAction));
    }

    private static void AddTargetControls(
        List<CardInteractionControlDescriptor> controls, DecisionComposer composer)
    {
        if (MulliganPrompt.IsOpening(composer.Prompt)
            || composer.UsesAutomaticTargetSelection
            || composer.Selected?.Targets is not { } request
            || request.IsGrouped
            || request.AllowRepeated)
        {
            return;
        }
        foreach (int target in request.Legal.Distinct().OrderBy(id => id))
        {
            bool selected = composer.Targets.Contains(target);
            controls.Add(new CardInteractionControlDescriptor(
                target,
                CardInteractionIntent.Target,
                selected ? "✓ TARGET" : "◇ TARGET",
                selected ? CardInteractionCue.SelectedTarget : CardInteractionCue.LegalTarget));
        }
    }

    private static void AddActionControls(
        List<CardInteractionControlDescriptor> controls, PromptPresentation prompt)
    {
        foreach (IGrouping<int, AffordancePresentation> actions in prompt.Affordances
                     .Where(affordance => affordance.CardAnchorId is not null
                         && affordance.Illegal is null)
                     .GroupBy(affordance => affordance.CardAnchorId!.Value)
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

        CostOption cost = selected.CostOptions[composer.SelectedCost];
        if (!composer.CostApplies(cost))
        {
            return;
        }

        foreach (int generator in cost.Generators
                     .Select(source => source.Effect).Distinct().OrderBy(id => id))
        {
            bool selectedGenerator = composer.Resources.Contains(generator);
            controls.Add(new CardInteractionControlDescriptor(
                generator,
                CardInteractionIntent.Generator,
                selectedGenerator ? "✓ PAY" : "◇ PAY",
                selectedGenerator
                    ? CardInteractionCue.SelectedGenerator
                    : CardInteractionCue.LegalGenerator));
        }
    }
}
