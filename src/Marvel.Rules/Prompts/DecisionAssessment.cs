using Marvel.Rules.Play;

namespace Marvel.Rules.Prompts;

/// <summary>One authority for complete prompt answers in gameplay, drafts and replay.</summary>
public static class DecisionAssessment
{
    /// <summary>
    /// Assesses one answer against its current prompt. A draft may require its exact
    /// selected cost; committed answers may satisfy any complete applicable option.
    /// </summary>
    public static DecisionAssessmentResult Assess(
        Prompt prompt, Decision answer, int? actor = null, int? costIndex = null)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(answer);
        if (answer.IsDecline) return Decline(prompt, answer, actor);
        Affordance[] matches = [.. prompt.Affordances.Where(option => option.Id == answer.Affordance)];
        if (matches.Length != 1) return new(DecisionRejection.MissingOffer);
        Affordance selected = matches[0];
        if (actor is { } seat && seat != ActingSeat(prompt, selected))
            return new(DecisionRejection.UnauthorizedActor);
        if (!selected.IsLegal) return new(DecisionRejection.IllegalOffer, selected.Illegal);
        if (!TargetsAllowed(selected, answer.Targets)) return new(DecisionRejection.InvalidTargets);
        return Payment(selected, answer, costIndex);
    }

    /// <summary>The answering seat, including another player's offered Action.</summary>
    public static int ActingSeat(Prompt prompt, Affordance selected) =>
        string.Equals(selected.Verb, Game.ActionVerb, StringComparison.Ordinal)
            && selected.AnchorPlayer >= 0 ? selected.AnchorPlayer : prompt.Player;

    /// <summary>Whether an ordered selection satisfies this offer's target request.</summary>
    public static bool TargetsAllowed(Affordance selected, IReadOnlyList<int> targets) =>
        selected.Targets is null ? targets.Count == 0 : selected.Targets.Allows(targets);

    /// <summary>Whether a price applies to the action anchor or selected targets.</summary>
    public static bool CostApplies(Affordance selected, CostOption cost, IReadOnlyList<int> targets) =>
        cost.Target == 0 || cost.Target == selected.AnchorId || targets.Contains(cost.Target);

    /// <summary>Whether values satisfy exactly this option's variable declarations.</summary>
    public static bool VariablesAllowed(CostOption cost, IReadOnlyDictionary<string, long> values)
    {
        IReadOnlyList<VariableRequest> requested = cost.VariableRequests;
        return values.Count == requested.Count
            && requested.Select(variable => variable.Name).Distinct(StringComparer.Ordinal).Count() == requested.Count
            && requested.All(variable => values.TryGetValue(variable.Name, out long value)
                && variable.Allows(value));
    }

    private static DecisionAssessmentResult Decline(Prompt prompt, Decision answer, int? actor)
    {
        if (actor is { } seat && seat != prompt.Player) return new(DecisionRejection.UnauthorizedActor);
        if (!prompt.Cancellable) return new(DecisionRejection.DeclineNotAllowed);
        return answer.Targets.Count == 0 && HasNoPayment(answer)
            ? default : new(DecisionRejection.UnexpectedAnswerData);
    }

    private static bool HasNoPayment(Decision answer) =>
        answer.Spent.Count == 0 && answer.DefinedValues.Count == 0 && answer.Allocated.Count == 0;

    private static DecisionAssessmentResult Payment(Affordance selected, Decision answer, int? costIndex)
    {
        if (selected.CostOptions.Count == 0)
            return HasNoPayment(answer) ? default : new(DecisionRejection.UnexpectedAnswerData);
        if (costIndex is { } index)
            return index < 0 || index >= selected.CostOptions.Count
                ? new(DecisionRejection.CostSelectionRequired)
                : AssessCost(selected, selected.CostOptions[index], answer);

        // Alternatives are independent offers: variables and payment must match
        // the same option. Combining parts of different options invents a cost.
        var failure = new DecisionAssessmentResult(DecisionRejection.CostTargetMismatch);
        foreach (CostOption cost in selected.CostOptions)
        {
            DecisionAssessmentResult result = AssessCost(selected, cost, answer);
            if (result.IsAccepted) return result;
            if (result.Rejection > failure.Rejection) failure = result;
        }
        return failure;
    }

    private static DecisionAssessmentResult AssessCost(Affordance selected, CostOption cost, Decision answer)
    {
        if (!CostApplies(selected, cost, answer.Targets)) return new(DecisionRejection.CostTargetMismatch);
        if (!VariablesAllowed(cost, answer.DefinedValues)) return new(DecisionRejection.InvalidVariables);
        return ResourcePayment.Allows(cost, answer.Spent, answer.DefinedValues, answer.Allocated)
            ? default : new(DecisionRejection.InvalidPayment);
    }
}
