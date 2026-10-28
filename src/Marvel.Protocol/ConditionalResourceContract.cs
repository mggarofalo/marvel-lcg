using Marvel.Rules.Prompts;

namespace Marvel.Protocol;

// Reject malformed conditional semantics before a response reaches a client.
internal static class ConditionalResourceContract
{
    internal static bool IsValid(Prompt? prompt) => prompt?.Affordances is not { } affordances
        || affordances.All(offer => offer?.Costs is not { } costs
            || costs.All(ValidCost));

    private static bool ValidCost(CostOption? cost) => cost is null || cost.Sources is not { } sources
        || sources.All(source => source.ConditionalBonus is not { } bonus
            || bonus.IsValid(cost.ResourceCosts.Count));
}
