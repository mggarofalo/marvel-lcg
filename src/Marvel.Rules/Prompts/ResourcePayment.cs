using Marvel.Rules.Play;
using static Marvel.Rules.Prompts.ResourcePaymentRequirements;

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
    /// <summary>The engine-resolved typed requirements, or null until the cost is defined.</summary>
    public static string? RequiredResources(
        ResourceCost cost, IReadOnlyDictionary<string, long>? values = null) =>
        Resolve(cost, values)?.Required;

    /// <summary>Whether a generator can supply any slot in an offered cost.</summary>
    /// <remarks>
    /// This is partial-payment guidance, not a restriction on legal overpayment.
    /// An unresolved variable keeps the source available until its cost is known.
    /// </remarks>
    public static bool CanContribute(
        CostOption option, int effect, IReadOnlyDictionary<string, long>? values = null)
    {
        ArgumentNullException.ThrowIfNull(option);
        ResourceSource[] sources = [.. option.Generators.Where(source => source.Effect == effect)];
        if (sources.Length != 1 || sources[0].Generates.Length == 0) return false;
        return ContributesTo(option.ResourceCosts, sources[0], values)
            || option.Components is null && option.HasAlternative
            && ContributesTo([new ResourceCost(option.OrCost, option.OrRule)], sources[0], values);
    }

    private static bool ContributesTo(
        IReadOnlyList<ResourceCost> costs, ResourceSource source,
        IReadOnlyDictionary<string, long>? values)
    {
        List<Slot>? slots = Slots(costs, values);
        return slots is null || slots.Any(slot => source.PossibleResources.Any(icon =>
            Accepts(icon, slot.Required, slot.Printed)));
    }

    /// <summary>
    /// Whether an explicit player allocation pays the selected option with
    /// exactly the named generators and variable values.
    /// </summary>
    public static bool Allows(
        CostOption option,
        IReadOnlyList<int> paying,
        IReadOnlyDictionary<string, long>? values,
        IReadOnlyList<ResourceAllocation> allocations)
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(paying);
        ArgumentNullException.ThrowIfNull(allocations);

        if (paying.Distinct().Count() != paying.Count)
        {
            return false;
        }

        var selected = new List<ResourceSource>(paying.Count);
        foreach (int effect in paying)
        {
            List<ResourceSource> matches = option.Generators
                .Where(source => source.Effect == effect)
                .ToList();
            if (matches.Count != 1)
            {
                return false;
            }

            selected.Add(matches[0]);
        }

        if (Allows(option.ResourceCosts, selected, values, allocations))
        {
            return true;
        }

        return option.Components is null
            && option.HasAlternative
            && Allows(
                [new ResourceCost(option.OrCost, option.OrRule)],
                selected,
                values,
                allocations);
    }

    private static bool Allows(
        IReadOnlyList<ResourceCost> costs,
        IReadOnlyList<ResourceSource> selected,
        IReadOnlyDictionary<string, long>? values,
        IReadOnlyList<ResourceAllocation> allocations)
    {
        var sources = selected.Select(source => source.Effect).ToHashSet();
        var slots = new List<Slot>();
        var paid = Enumerable.Range(0, costs.Count)
            .Select(_ => new System.Text.StringBuilder())
            .ToList();
        foreach (ResourceAllocation allocation in allocations)
        {
            if (!ApplyAllocation(allocation, costs, sources, paid, slots)) return false;
        }

        for (int index = 0; index < costs.Count; index++)
        {
            if (!ComponentPaid(costs[index], paid[index], values)) return false;
        }

        // Source and declaration are the player's commitments. Physical icon
        // assignment must satisfy all components together, including printed
        // requirements, rather than depending on allocation enumeration order.
        return new ResourceAllocationSearch(slots, selected).CanAllocate();
    }

    /// <summary>Output of one quoted source for the submitted component allocation.</summary>
    public static string GeneratedBy(ResourceSource source, IReadOnlyList<ResourceAllocation> allocations) =>
        source.Generates + (source.ConditionalBonus is { } bonus
            && allocations.Any(allocation => allocation.Source == source.Effect
                && allocation.PaidAs.Length > 0 && bonus.AppliesTo(allocation.Cost))
            ? bonus.Generates : string.Empty);

    private static bool ApplyAllocation(
        ResourceAllocation allocation, IReadOnlyList<ResourceCost> costs,
        HashSet<int> sources, List<System.Text.StringBuilder> paid, List<Slot> slots)
    {
        if (!sources.Contains(allocation.Source)
            || allocation.Cost < 0 || allocation.Cost >= costs.Count
            || allocation.PaidAs.Length == 0
            || !allocation.PaidAs.All(Resources.Types.Contains)) return false;
        ResourceCost cost = costs[allocation.Cost];
        foreach (char declared in allocation.PaidAs)
        {
            paid[allocation.Cost].Append(declared);
            slots.Add(new Slot(allocation.Cost, declared, cost.Printed,
                cost.DistinctTypes, allocation.Source));
        }
        return true;
    }

    private static bool ComponentPaid(ResourceCost cost, System.Text.StringBuilder paid,
        IReadOnlyDictionary<string, long>? values)
    {
        if (Resolve(cost, values) is not { } resolved) return false;
        string assigned = paid.ToString();
        return (!cost.DistinctTypes || assigned.Distinct().Count() == assigned.Length)
            && assigned.Length == resolved.Amount
            && Resources.PaysDeclared(assigned, resolved.Amount, resolved.Required);
    }

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
        List<Slot>? slots = Slots(costs, values);
        if (slots is null) return null;

        return new ResourceAllocationSearch(slots, sources).Allocate();
    }
}
