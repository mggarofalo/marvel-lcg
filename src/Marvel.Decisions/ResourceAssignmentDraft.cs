using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

/// <summary>Drafts generator choices and individual icon assignments for an offered cost.</summary>
internal sealed class ResourceAssignmentDraft
{
    private readonly List<int> resources = [];
    private readonly List<ResourceIconAssignment> assignments = [];

    internal IReadOnlyList<int> Resources => resources;
    internal IReadOnlyList<ResourceIconAssignment> Assignments => assignments;

    internal void Clear()
    {
        resources.Clear();
        assignments.Clear();
    }

    internal bool UsesAutomaticAllocation(CostOption? cost)
    {
        if (cost is null || cost.ResourceCosts.Count != 1 || resources.Count == 0)
        {
            return false;
        }

        bool hasWild = false;
        bool valid = resources.All(effect =>
        {
            ResourceSource[] matches =
                [.. cost.Generators.Where(source => source.Effect == effect)];
            if (matches.Length != 1)
            {
                return false;
            }

            hasWild |= matches[0].Generates.Contains(
                Marvel.Rules.Play.Resources.Wild);
            return true;
        });
        return valid && (!hasWild || !cost.DeclarationSensitive);
    }

    internal void Toggle(int effect, CostOption? cost, IReadOnlyDictionary<string, long> values)
    {
        bool wasAutomatic = UsesAutomaticAllocation(cost);
        int index = resources.IndexOf(effect);
        if (index >= 0)
        {
            resources.RemoveAt(index);
            assignments.RemoveAll(assignment => assignment.Source == effect);
        }
        else
        {
            resources.Add(effect);
        }

        bool isAutomatic = UsesAutomaticAllocation(cost);
        bool usesSuggestion = ResourcePaymentDraft.CanSuggest(cost, resources);
        if (wasAutomatic || isAutomatic || usesSuggestion)
        {
            assignments.Clear();
        }

        if (isAutomatic || usesSuggestion)
        {
            Apply(cost!, values);
        }
    }

    internal void Assign(
        CostOption? selected,
        int source,
        int icon,
        int? cost,
        char? paidAs)
    {
        if ((cost is null) != (paidAs is null))
        {
            throw new ArgumentException("A resource assignment needs both a cost and a type.");
        }

        ResourceSource generator = selected?.Generators.SingleOrDefault(candidate =>
            candidate.Effect == source) ?? default;
        if (!AssignmentIsOffered(selected, generator, source, icon, cost, paidAs))
        {
            throw new ArgumentOutOfRangeException(nameof(source), source,
                "resource assignment is not offered by the selected cost");
        }

        assignments.RemoveAll(assignment =>
            assignment.Source == source && assignment.Icon == icon);
        if (cost is not null && paidAs is not null)
        {
            assignments.Add(new ResourceIconAssignment(
                source, icon, cost.Value, paidAs.Value));
        }
    }

    private bool AssignmentIsOffered(
        CostOption? costOption,
        ResourceSource generator,
        int source,
        int icon,
        int? cost,
        char? paidAs)
    {
        if (costOption is null || !resources.Contains(source) || generator.Generates is null)
        {
            return false;
        }
        return IconIsOffered(generator, icon)
            && CostIsOffered(costOption, cost)
            && PaidTypeIsOffered(generator, icon, paidAs);
    }

    private static bool IconIsOffered(ResourceSource generator, int icon) =>
        icon >= 0 && icon < generator.Generates.Length;

    private static bool CostIsOffered(CostOption option, int? cost) =>
        cost is null || cost >= 0 && cost < option.ResourceCosts.Count;

    private static bool PaidTypeIsOffered(
        ResourceSource generator, int icon, char? paidAs) =>
        paidAs is null
        || Marvel.Rules.Play.Resources.Types.Contains(paidAs.Value)
            && (generator.Generates[icon] == Marvel.Rules.Play.Resources.Wild
                || generator.Generates[icon] == paidAs);

    internal IReadOnlyList<ResourceAllocation> CollapseAssignments()
    {
        var order = new List<(int Source, int Cost)>();
        var paid = new Dictionary<(int Source, int Cost), System.Text.StringBuilder>();
        foreach (ResourceIconAssignment assignment in assignments
                     .OrderBy(assignment => resources.IndexOf(assignment.Source))
                     .ThenBy(assignment => assignment.Icon))
        {
            var key = (assignment.Source, assignment.Cost);
            if (!paid.TryGetValue(key, out System.Text.StringBuilder? declared))
            {
                declared = new System.Text.StringBuilder();
                paid.Add(key, declared);
                order.Add(key);
            }

            declared.Append(assignment.PaidAs);
        }

        return [.. order.Select(key =>
            new ResourceAllocation(key.Source, key.Cost, paid[key].ToString()))];
    }

    private void Apply(CostOption cost, IReadOnlyDictionary<string, long> values)
    {
        IReadOnlyList<ResourceAllocation>? allocation =
            ResourcePaymentDraft.Allocate(cost, resources, values);
        if (allocation is null)
        {
            return;
        }

        foreach (ResourceAllocation payment in allocation)
        {
            ResourceSource source = cost.Generators.Single(candidate =>
                candidate.Effect == payment.Source);
            var used = new HashSet<int>();
            foreach (char paidAs in payment.PaidAs)
            {
                int icon = Enumerable.Range(0, source.Generates.Length)
                    .Where(index => !used.Contains(index))
                    .OrderBy(index => source.Generates[index] == paidAs ? 0 : 1)
                    .First(index => source.Generates[index] == paidAs
                        || source.Generates[index] == Marvel.Rules.Play.Resources.Wild);
                used.Add(icon);
                assignments.Add(new ResourceIconAssignment(
                    source.Effect, icon, payment.Cost, paidAs));
            }
        }
    }

    internal void Refresh(CostOption? cost, IReadOnlyDictionary<string, long> values)
    {
        if (UsesAutomaticAllocation(cost) || ResourcePaymentDraft.CanSuggest(cost, resources))
        {
            assignments.Clear();
            Apply(cost!, values);
        }
    }
}
