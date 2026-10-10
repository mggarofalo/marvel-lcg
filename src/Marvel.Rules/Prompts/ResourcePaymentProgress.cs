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
        if (cost.Printed || cost.RepeatedResource is not null || cost.Rule is { Count: > 0 }
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
}
