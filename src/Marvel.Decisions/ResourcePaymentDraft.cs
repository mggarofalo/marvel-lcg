using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

/// <summary>Builds reversible payment drafts from engine-authored preferences.</summary>
internal static class ResourcePaymentDraft
{
    internal static IReadOnlyList<ResourceAllocation>? Allocate(
        CostOption option,
        IReadOnlyList<int> paying,
        IReadOnlyDictionary<string, long>? values = null)
    {
        ArgumentNullException.ThrowIfNull(option);
        char[] preferred = [.. option.PreferredResourceTypes.Distinct()];
        if (preferred.Length != 1 || option.ResourceCosts.Count != 1)
        {
            return ResourcePayment.Allocate(option, paying, values);
        }

        ResourceCost component = option.ResourceCosts[0];
        string required = string.Concat(component.Rule ?? []);
        if (!RequiredHasRoom(component.Cost, required, values)
            || required.Contains(preferred[0]))
        {
            return ResourcePayment.Allocate(option, paying, values);
        }

        var suggested = option.Components is null
            ? option with { Rule = [required + preferred[0]] }
            : option with
            {
                Components = [component with { Rule = [required + preferred[0]] }],
            };
        return ResourcePayment.Allocate(suggested, paying, values);
    }

    private static bool RequiredHasRoom(
        string cost, string required, IReadOnlyDictionary<string, long>? values) =>
        (long.TryParse(cost, out long amount)
            || values is not null && values.TryGetValue(cost, out amount))
        && required.Length < amount;
}
