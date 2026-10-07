using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

/// <summary>Explains resource staging using engine-owned payment requirements.</summary>
public static class DecisionResourceEligibility
{
    /// <summary>Whether this source can pay any part of the selected cost.</summary>
    public static bool Contributes(DecisionComposer composer, int effect) =>
        composer.SelectedCostOption() is { } cost && composer.CostApplies(cost)
        && ResourcePayment.CanContribute(cost, effect, composer.Values);

    /// <summary>
    /// Whether to add or remove a source in the guided draft. Extra sources may
    /// be staged after payment is satisfied; selected sources can always be removed.
    /// </summary>
    public static bool CanToggle(DecisionComposer composer, int effect) =>
        composer.Resources.Contains(effect)
        || composer.SelectedCostOption() is { } cost && composer.CostApplies(cost)
        && cost.Generators.Any(source => source.Effect == effect)
        && (Contributes(composer, effect) || composer.Progress().Payment.IsSatisfied);
}
