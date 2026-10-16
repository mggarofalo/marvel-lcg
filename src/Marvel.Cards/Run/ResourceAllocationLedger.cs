using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Validates the explicit assignment of generated icons to simultaneous costs.</summary>
internal sealed class ResourceAllocationLedger
{
    private readonly List<ResourceCost> components;
    private readonly Card card;
    private readonly Dictionary<int, List<char>> remaining;
    private readonly List<System.Text.StringBuilder> paid;

    internal ResourceAllocationLedger(
        IReadOnlyList<ResourceSource> generators, IReadOnlyList<int> paying,
        List<ResourceCost> components, Card card)
    {
        this.components = components;
        this.card = card;
        var selected = paying.ToHashSet();
        remaining = generators.Where(source => selected.Contains(source.Effect))
        .ToDictionary(
            source => source.Effect,
            source => source.Generates.ToList());
        paid = Enumerable.Range(0, components.Count)
            .Select(_ => new System.Text.StringBuilder()).ToList();
    }

    internal string Allocate(IReadOnlyList<ResourceAllocation> allocations)
    {
        foreach (var allocation in allocations) Allocate(allocation);
        ValidateComponents();
        return string.Concat(paid.Select(component => component.ToString()));
    }

    private void Allocate(ResourceAllocation allocation)
    {
        if (!remaining.TryGetValue(allocation.Source, out var available)
            || allocation.Cost < 0 || allocation.Cost >= components.Count
            || allocation.PaidAs.Length == 0)
            throw new RulesNotImplementedException(
                $"event {card.ObjectId} carries an invalid resource allocation");
        foreach (char declared in allocation.PaidAs)
            Consume(allocation, available, declared);
    }

    private void Consume(ResourceAllocation allocation, List<char> available, char declared)
    {
        if (!Resources.Types.Contains(declared))
            throw new RulesNotImplementedException(
                $"event {card.ObjectId} declares unknown resource '{declared}'");
        int icon = available.IndexOf(declared);
        if (icon < 0 && !components[allocation.Cost].Printed
            && declared != Resources.Wild)
            icon = available.IndexOf(Resources.Wild);
        if (icon < 0)
            throw new RulesNotImplementedException(
                $"event {card.ObjectId} allocates '{declared}' from generator {allocation.Source}, which cannot produce it");
        available.RemoveAt(icon);
        paid[allocation.Cost].Append(declared);
    }

    private void ValidateComponents()
    {
        for (int index = 0; index < components.Count; index++)
        {
            if (!long.TryParse(components[index].Cost,
                    System.Globalization.CultureInfo.InvariantCulture, out long amount))
                throw new RulesNotImplementedException(
                    $"event {card.ObjectId} has a non-numeric allocation component");
            string assigned = paid[index].ToString();
            string required = string.Concat(components[index].Rule ?? []);
            if (assigned.Length != amount
                || !Resources.PaysDeclared(assigned, amount, required))
                throw new RulesNotImplementedException(
                    $"event {card.ObjectId} assigns '{assigned}' to cost {index}, which costs {amount}"
                    + (required.Length > 0 ? $" requiring '{required}'" : string.Empty));
        }
    }
}
