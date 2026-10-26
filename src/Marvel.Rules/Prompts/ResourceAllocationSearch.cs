using Marvel.Rules.Play;
using static Marvel.Rules.Prompts.ResourcePaymentRequirements;

namespace Marvel.Rules.Prompts;

/// <summary>Finds one deterministic allocation without changing the player's payment.</summary>
internal sealed class ResourceAllocationSearch
{
    private readonly List<Slot> slots;
    private readonly List<Icon> icons;
    private readonly bool[] used;
    private readonly Choice[] choices;
    private readonly bool[] assigned;

    internal ResourceAllocationSearch(List<Slot> slots, IReadOnlyList<ResourceSource> sources)
    {
        this.slots = slots;
        icons = sources.SelectMany(source => source.Generates.Select(resource =>
            new Icon(source.Effect, resource))).ToList();
        used = new bool[icons.Count];
        choices = new Choice[slots.Count];
        assigned = new bool[slots.Count];
    }

    internal IReadOnlyList<ResourceAllocation>? Allocate() =>
        CanAllocate() ? Collapse() : null;

    internal bool CanAllocate() => icons.Count >= slots.Count && Search(0);

    private bool Search(int slotIndex)
    {
        if (slotIndex == slots.Count) return true;
        Slot slot = slots[slotIndex];
        foreach (int index in Candidates(slot))
        {
            used[index] = true;
            foreach (char declared in Declarations(slot, icons[index]))
            {
                if (AlreadyPaid(slotIndex, slot, declared)) continue;
                assigned[slotIndex] = true;
                choices[slotIndex] = new Choice(icons[index].Source, slot.Component, declared);
                if (Search(slotIndex + 1)) return true;
            }
            used[index] = false;
            assigned[slotIndex] = false;
        }
        return false;
    }

    private bool AlreadyPaid(int slotIndex, Slot slot, char declared) =>
        slot.DistinctTypes && Enumerable.Range(0, slotIndex).Any(index => assigned[index]
            && choices[index].Component == slot.Component && choices[index].Declared == declared);

    private static IEnumerable<char> Declarations(Slot slot, Icon icon) =>
        ResourcePaymentRequirements.Declarations(slot, icon.Printed);

    private IEnumerable<int> Candidates(Slot slot)
    {
        IEnumerable<int> candidates = Enumerable.Range(0, icons.Count)
            .Where(index => !used[index]
                && (slot.Source is null || icons[index].Source == slot.Source)
                && Accepts(icons[index].Printed, slot.Required, slot.Printed));
        return slot.Required is { } required
            ? candidates.OrderBy(index => icons[index].Printed == required ? 0 : 1)
            : candidates;
    }

    private IReadOnlyList<ResourceAllocation> Collapse()
    {
        var order = new List<(int Source, int Cost)>();
        var paid = new Dictionary<(int Source, int Cost), System.Text.StringBuilder>();
        foreach (Choice choice in choices)
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

    private readonly record struct Icon(int Source, char Printed);
    private readonly record struct Choice(int Source, int Component, char Declared);
}
