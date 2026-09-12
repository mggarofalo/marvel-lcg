using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

/// <summary>Projects a decision draft into render-safe completion progress.</summary>
internal static class DecisionProgressProjection
{
    internal static DecisionProgressPresentation From(DecisionComposer composer)
    {
        bool ready = composer.TryBuild(out _, out string? error);
        return new DecisionProgressPresentation(
            DecisionTargetProgress.Compute(composer.Selected, composer.Targets),
            Payment(composer),
            ready,
            error);
    }

    private static PaymentProgress Payment(DecisionComposer composer)
    {
        if (composer.Selected is null)
        {
            return new PaymentProgress(
                CostSelectionState.Unavailable,
                null,
                0,
                0,
                0,
                0,
                0,
                0,
                false);
        }
        if (composer.Selected.CostOptions.Count == 0)
        {
            return NoCost(composer);
        }

        CostOption? cost = composer.SelectedCostOption();
        if (cost is null)
        {
            return Empty(composer, CostSelectionState.Required);
        }

        int generatedIcons = composer.Resources.Sum(effect =>
            cost.Generators
                .Where(generator => generator.Effect == effect)
                .Sum(generator => generator.Generates.Length));
        VariableRequest[] requested = cost.VariableRequests.ToArray();
        bool variablesSatisfied = composer.Values.Count == requested.Length
            && requested.All(variable =>
                composer.Values.TryGetValue(variable.Name, out long value)
                && variable.Allows(value));
        bool paymentSatisfied = composer.CostApplies(cost)
            && variablesSatisfied
            && ResourcePayment.Allows(
                cost, composer.Resources, composer.Values, composer.CollapseAssignments());
        return new PaymentProgress(
            CostSelectionState.Selected,
            composer.SelectedCost,
            composer.Selected.CostOptions.Count,
            composer.Resources.Count,
            generatedIcons,
            composer.Assignments.Count,
            composer.Values.Count,
            requested.Length,
            paymentSatisfied);
    }

    private static PaymentProgress Empty(
        DecisionComposer composer, CostSelectionState state) => new(
        state,
        null,
        composer.Selected?.CostOptions.Count ?? 0,
        composer.Resources.Count,
        0,
        composer.Assignments.Count,
        composer.Values.Count,
        0,
        false);

    private static PaymentProgress NoCost(DecisionComposer composer) => new(
        CostSelectionState.NotRequired,
        null,
        0,
        composer.Resources.Count,
        0,
        composer.Assignments.Count,
        composer.Values.Count,
        0,
        composer.Resources.Count == 0
            && composer.Assignments.Count == 0
            && composer.Values.Count == 0);
}
