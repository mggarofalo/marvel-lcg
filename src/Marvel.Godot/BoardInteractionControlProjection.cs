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
        if (composer is null || prompt is null || CardPaymentPresentation.UsesModal(composer))
        {
            return [];
        }

        var controls = new List<CardInteractionControlDescriptor>();
        if (composer.Selected is null)
        {
            AddActionControls(controls, prompt);
        }
        AddTargetControls(controls, composer);
        AddGeneratorControls(controls, composer);
        return controls;
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
                TargetLabel(composer, selected),
                selected ? CardInteractionCue.SelectedTarget : CardInteractionCue.LegalTarget,
                Description: request.Details?.GetValueOrDefault(target)));
        }
    }

    private static string TargetLabel(DecisionComposer composer, bool selected) =>
        composer.Selected?.Verb == Marvel.Rules.Play.Game.EndPhaseVerb
            ? selected ? "✓ Discard staged" : "Stage discard"
            : selected ? "✓ Chosen" : "Select target";

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
                actions.Count() == 1
                    ? ActionEntry(actions.Single())
                    : $"Actions · {actions.Count()}",
                CardInteractionCue.OfferedAction,
                Description: actions.Count() == 1 ? DecisionCopy.ActionSummary(actions.Single()) : null));
        }
    }


    private static string ActionEntry(AffordancePresentation action) => action.PlaysCard ? "Play" : action.Verb switch
    {
        "Attack" or "Thwart" or "Recover" or "Defend" or "Defense" or "Play" => action.Verb,
        _ => PromptPresentation.Words(action.DisplayLabel ?? action.Label),
    };

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
                selectedGenerator ? "✓ Resource selected" : "Use resource",
                selectedGenerator
                    ? CardInteractionCue.SelectedGenerator
                    : CardInteractionCue.LegalGenerator));
        }
    }
}
