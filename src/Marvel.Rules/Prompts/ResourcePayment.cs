using Marvel.Rules.Play;

namespace Marvel.Rules.Prompts;

/// <summary>Builds one explicit, deterministic allocation for a chosen payment.</summary>
/// <remarks>
/// This is a policy helper, not an engine default. The rulebook gives the
/// player the allocation choice; callers use this method when they themselves
/// are the player, such as a simulation policy. A command sent by a client
/// still has to carry its own choice whenever the allocation is observable.
/// </remarks>
public static class ResourcePayment
{
    /// <summary>
    /// Allocates icons from <paramref name="paying"/> among the simultaneous
    /// components of <paramref name="option"/>, or returns <see langword="null"/>
    /// when that payment cannot satisfy the option.
    /// </summary>
    public static IReadOnlyList<ResourceAllocation>? Allocate(
        CostOption option,
        IReadOnlyList<int> paying,
        IReadOnlyDictionary<string, long>? values = null)
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(paying);

        if (paying.Distinct().Count() != paying.Count)
        {
            return null;
        }

        var selected = new List<ResourceSource>(paying.Count);
        foreach (int effect in paying)
        {
            var matches = option.Generators
                .Where(source => source.Effect == effect)
                .ToList();
            if (matches.Count != 1)
            {
                return null;
            }
            selected.Add(matches[0]);
        }

        var primary = option.ResourceCosts;
        var allocated = Allocate(primary, selected, values);
        if (allocated is not null || option.Components is not null || !option.HasAlternative)
        {
            return allocated;
        }

        return Allocate([new ResourceCost(option.OrCost, option.OrRule)], selected, values);
    }

    private static IReadOnlyList<ResourceAllocation>? Allocate(
        IReadOnlyList<ResourceCost> costs,
        IReadOnlyList<ResourceSource> sources,
        IReadOnlyDictionary<string, long>? values)
    {
        var slots = new List<Slot>();
        for (int component = 0; component < costs.Count; component++)
        {
            if (!Amount(costs[component].Cost, values, out long amount)
                || amount < 0 || amount > int.MaxValue)
            {
                return null;
            }

            string required = string.Concat(costs[component].Rule ?? []);
            if (required.Length > amount
                || required.Any(resource => !Resources.Types.Contains(resource)))
            {
                return null;
            }

            slots.AddRange(required.Select(resource => new Slot(component, resource)));
            slots.AddRange(Enumerable.Repeat(
                new Slot(component, Required: null),
                checked((int)amount - required.Length)));
        }

        var icons = sources
            .SelectMany(source => source.Generates.Select(resource =>
                new Icon(source.Effect, resource)))
            .ToList();
        if (icons.Count < slots.Count)
        {
            return null;
        }

        var used = new bool[icons.Count];
        var choices = new Choice[slots.Count];
        return Search(0) ? Collapse() : null;

        bool Search(int slotIndex)
        {
            if (slotIndex == slots.Count)
            {
                return true;
            }

            var slot = slots[slotIndex];
            IEnumerable<int> candidates = Enumerable.Range(0, icons.Count)
                .Where(index => !used[index] && Accepts(icons[index].Printed, slot.Required));
            if (slot.Required is { } required)
            {
                // Spend the exact type before a wild. This is deterministic
                // and preserves wilds for later required slots when possible.
                candidates = candidates.OrderBy(index => icons[index].Printed == required ? 0 : 1);
            }

            foreach (int index in candidates)
            {
                used[index] = true;
                char declared = slot.Required ?? icons[index].Printed;
                choices[slotIndex] = new Choice(icons[index].Source, slot.Component, declared);
                if (Search(slotIndex + 1))
                {
                    return true;
                }
                used[index] = false;
            }
            return false;
        }

        IReadOnlyList<ResourceAllocation> Collapse()
        {
            var order = new List<(int Source, int Cost)>();
            var paid = new Dictionary<(int Source, int Cost), System.Text.StringBuilder>();
            foreach (var choice in choices)
            {
                var key = (choice.Source, choice.Component);
                if (!paid.TryGetValue(key, out var declared))
                {
                    declared = new System.Text.StringBuilder();
                    paid.Add(key, declared);
                    order.Add(key);
                }
                declared.Append(choice.Declared);
            }

            return [.. order.Select(key =>
                new ResourceAllocation(key.Source, key.Cost, paid[key].ToString()))];
        }
    }

    private static bool Amount(
        string written,
        IReadOnlyDictionary<string, long>? values,
        out long amount) =>
        long.TryParse(
            written,
            System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture,
            out amount)
        || values is not null && values.TryGetValue(written, out amount);

    private static bool Accepts(char printed, char? required) =>
        required is null || printed == required || printed == Resources.Wild;

    private readonly record struct Slot(int Component, char? Required);
    private readonly record struct Icon(int Source, char Printed);
    private readonly record struct Choice(int Source, int Component, char Declared);
}
