using Marvel.Rules.Prompts;

namespace Marvel.View;

/// <summary>Authorized submitted intention, usable only after that exact decision succeeds.</summary>
public sealed record DecisionReceiptContext(string Commitment, IReadOnlyList<int> Anchors)
{
    /// <summary>The authorized offered card source, distinct from payment and target anchors.</summary>
    public int? SourceCardId { get; init; }
    /// <summary>The targets explicitly submitted for this decision.</summary>
    public IReadOnlyList<int> TargetCardIds { get; init; } = [];
    /// <summary>The submitted engine command, distinct from the player-facing wording.</summary>
    public string? ActionVerb { get; init; }
    /// <summary>Concise accepted action wording from the offered command and authorized participants.</summary>
    public string? OccurrenceTitle { get; init; }
    /// <summary>Captures offered meaning and chosen resource sources without claiming acceptance.</summary>
    public static DecisionReceiptContext? From(
        Prompt prompt, WorldDescriptor world, int affordance,
        IReadOnlyList<int> targets, IReadOnlyList<int> resources)
    {
        string actor = PendingSituationPresentation.SeatName(world, prompt.Player);
        if (affordance < 0)
            return new($"{actor}: {prompt.DeclineLabel}.", prompt.ContextCardIds);
        Affordance? chosen = prompt.Affordances.FirstOrDefault(option => option.Id == affordance);
        if (chosen is null || chosen.Illegal is not null) return null;
        AffordancePresentation offered = AffordancePresenter.Present(chosen, world);
        string label = CommitmentLabel(offered, chosen, targets, world);
        var available = chosen.CostOptions.SelectMany(cost => cost.Generators)
            .DistinctBy(generator => generator.Effect).ToDictionary(generator => generator.Effect);
        PaymentSourcePresentation[] payment = [.. resources.Where(available.ContainsKey)
            .Select(id => PaymentSourcePresentation.From(available[id], world))];
        string paid = payment.Length == 0 ? string.Empty
            : $" Payment sources: {string.Join(", ", payment.Select(item => item.Name))}.";
        string cause = chosen.PlaysCard && prompt.PublicKind is
            PublicDecisionKind.Interrupt or PublicDecisionKind.Response
                ? offered.SourceName ?? offered.Anchor : actor;
        return new($"{cause}: {label.TrimEnd('.')}.{paid}",
            [.. new[] { offered.CardAnchorId }.OfType<int>().Concat(targets)
                .Concat(payment.Select(item => item.Id)).Distinct()])
        {
            ActionVerb = chosen.Verb,
            SourceCardId = offered.CardAnchorId,
            TargetCardIds = targets.ToArray(),
            OccurrenceTitle = DecisionOccurrenceTitle.From(offered, targets, world),
        };
    }
    private static string CommitmentLabel(AffordancePresentation offered, Affordance chosen,
        IReadOnlyList<int> targets, WorldDescriptor world)
    {
        string source = offered.SourceName ?? offered.Anchor;
        string label = offered.CommitLabel ?? (offered.PlaysCard ? $"Play {source}"
            : offered.DisplayLabel ?? (offered.Label == chosen.Verb
                ? $"{offered.Verb} with {source}" : PromptPresentation.Words(offered.Label)));
        if (targets.Count > 0 && offered.CommitLabel is null)
            label += " → " + string.Join(" → ", targets.Select(id => PromptPresentation.Describe(id, world)));
        return label;
    }

}
