using Marvel.Rules.Prompts;

namespace Marvel.View;

/// <summary>Captures an authorized prompt without references to its mutable input collections.</summary>
internal static class PromptSnapshot
{
    internal static Prompt For(Prompt prompt, IReadOnlySet<int> readable) => prompt.WithoutExecutionBinding() with
    {
        Affordances = prompt.Affordances.Select(option => Copy(option, readable)).ToArray(),
        ContextCardIds = [.. prompt.ContextCardIds.Where(readable.Contains)],
        CauseCardIds = [.. prompt.CauseCardIds.Where(readable.Contains)],
    };

    private static Affordance Copy(Affordance option, IReadOnlySet<int> readable) => option with
    {
        Targets = option.Targets is { } targets ? Copy(targets) : null,
        Costs = option.Costs?.Select(Copy).ToArray(),
        SelectionSourceId = option.SelectionSourceId is { } source && readable.Contains(source)
            ? source : null,
        WindowChoices = readable.Contains(option.AnchorId)
            ? option.WindowChoices.Where(choice => choice.RecipientIds.All(readable.Contains))
                .Select(choice => choice with { RecipientIds = choice.RecipientIds.ToArray() }).ToArray()
            : [],
    };

    private static TargetRequest Copy(TargetRequest targets) => targets with
    {
        Legal = targets.Legal.ToArray(),
        Groups = Copy(targets.Groups),
        ExclusiveSets = Copy(targets.ExclusiveSets),
        MustIncludeTraits = targets.MustIncludeTraits?.ToArray(),
        MaximumOccurrences = targets.MaximumOccurrences?.ToDictionary(pair => pair.Key, pair => pair.Value),
        Details = targets.Details?.ToDictionary(pair => pair.Key, pair => pair.Value),
    };

    private static IReadOnlyList<int>[]? Copy(IReadOnlyList<IReadOnlyList<int>>? groups) =>
        groups?.Select(group => (IReadOnlyList<int>)group.ToArray()).ToArray();

    private static CostOption Copy(CostOption cost) => cost with
    {
        Rule = cost.Rule?.ToArray(),
        OrRule = cost.OrRule?.ToArray(),
        Sources = cost.Sources?.ToArray(),
        Variables = cost.Variables?.ToArray(),
        Components = cost.Components?.Select(component => component with
        {
            Rule = component.Rule?.ToArray(),
        }).ToArray(),
    };
}
