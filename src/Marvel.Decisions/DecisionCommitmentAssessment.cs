using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

/// <summary>Assesses whether an explicitly chosen draft has any payment choice left.</summary>
public static class DecisionCommitmentAssessment
{
    /// <summary>Uses engine validation without selecting payment, alternatives or future prompts.</summary>
    public static bool IsReadyWithoutPaymentChoice(DecisionComposer composer)
    {
        ArgumentNullException.ThrowIfNull(composer);
        return composer.Selected is { } option && NoPaymentChoice(composer, option)
            && composer.TryBuild(out _, out _);
    }

    private static bool NoPaymentChoice(DecisionComposer composer, Affordance option)
    {
        if (option.CostOptions.Count == 0) return true;
        if (option.CostOptions.Count != 1 || composer.Resources.Count != 0) return false;
        CostOption cost = option.CostOptions[0];
        return !cost.HasAlternative && !cost.DeclarationSensitive
            && cost.VariableRequests.Count == 0
            && !HasOptionalTargets(option.Targets)
            && ResourcePayment.Allows(cost, [], null, []);
    }

    private static bool HasOptionalTargets(TargetRequest? targets) => targets is not null
        && targets.Min != targets.Max && targets.Legal.Count > 0;
}
