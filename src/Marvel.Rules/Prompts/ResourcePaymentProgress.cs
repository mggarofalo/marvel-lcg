using System.Globalization;

namespace Marvel.Rules.Prompts;

/// <summary>Engine assessment of progress that does not require a completed allocation.</summary>
public static class ResourcePaymentProgress
{
    /// <summary>
    /// Additional icons needed for a single unrestricted numeric cost. Null
    /// means resource types, alternatives, variables or allocation need a richer
    /// explanation; zero is not authority to submit an answer.
    /// </summary>
    public static int? RemainingRequired(CostOption option, IReadOnlyList<int> paying)
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(paying);
        if (option.HasAlternative || option.VariableRequests.Count != 0
            || option.ResourceCosts.Count != 1) return null;
        ResourceCost cost = option.ResourceCosts[0];
        if (HasTypedRequirements(cost)
            || !int.TryParse(cost.Cost, NumberStyles.None, CultureInfo.InvariantCulture, out int required)
            || paying.Distinct().Count() != paying.Count) return null;
        int generated = 0;
        foreach (int effect in paying)
        {
            ResourceSource[] matches = [.. option.Generators.Where(source => source.Effect == effect)];
            if (matches.Length != 1) return null;
            generated += matches[0].Generates.Length;
        }
        return Math.Max(0, required - generated);
    }
    /// <summary>
    /// Shortages under one maximum-coverage suggestion, with typed slots preferred
    /// over generic slots. Null means the cost or selected sources are unresolved.
    /// The suggestion does not assign the player's draft or establish readiness.
    /// </summary>
    public static IReadOnlyList<ResourceComponentProgress>? Components(CostOption option,
        IReadOnlyList<int> paying, IReadOnlyDictionary<string, long>? values = null)
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(paying);
        if (option.HasAlternative || paying.Distinct().Count() != paying.Count) return null;
        var sources = new List<ResourceSource>();
        foreach (int effect in paying)
        {
            ResourceSource[] matches = [.. option.Generators.Where(source => source.Effect == effect)];
            if (matches.Length != 1) return null;
            sources.Add(matches[0]);
        }
        var slots = ResourcePaymentRequirements.Slots(option.ResourceCosts, values);
        if (slots is null) return null;
        // Repository policy: reserve scarce typed icons before showing generic shortages.
        slots = slots.OrderBy(slot => slot.Required is null ? 1 : 0).ToList();
        var uncovered = new ResourceCoverageSearch(slots, sources).Uncovered();
        return Array.AsReadOnly(option.ResourceCosts.Select((cost, index) =>
            new ResourceComponentProgress(index, slots.Count(slot => slot.Component == index),
                uncovered.Count(slot => slot.Component == index),
                string.Concat(uncovered.Where(slot => slot.Component == index && slot.Required is not null)
                    .Select(slot => slot.Required)), cost.Printed, cost.DistinctTypes)).ToArray());
    }

    private static bool HasTypedRequirements(ResourceCost cost) =>
        cost.DistinctTypes || cost.Printed || cost.RepeatedResource is not null
        || cost.Rule is { Count: > 0 };
}
