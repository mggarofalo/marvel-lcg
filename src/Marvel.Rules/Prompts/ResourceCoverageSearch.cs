using Marvel.Rules.Play;
using static Marvel.Rules.Prompts.ResourcePaymentRequirements;

namespace Marvel.Rules.Prompts;

/// <summary>Finds maximum partial coverage without choosing a committed allocation.</summary>
internal sealed class ResourceCoverageSearch
{
    private readonly List<Slot> slots;
    private readonly (int? Source, char Printed)[] groups;
    private readonly int[] remaining;
    private readonly int[] declared;
    private readonly Dictionary<string, Coverage> memo = new(StringComparer.Ordinal);

    internal ResourceCoverageSearch(List<Slot> slots, IReadOnlyList<ResourceSource> sources)
    {
        this.slots = slots;
        var boundSources = slots.Where(slot => slot.Source is not null)
            .Select(slot => slot.Source!.Value).ToHashSet();
        // Icons are interchangeable only when no source-bound slot distinguishes them.
        var grouped = sources.SelectMany(source => source.Generates.Select(printed =>
            (Source: boundSources.Contains(source.Effect) ? (int?)source.Effect : null, Printed: printed)))
            .GroupBy(icon => icon).ToArray();
        groups = grouped.Select(group => group.Key).ToArray();
        remaining = grouped.Select(group => group.Count()).ToArray();
        declared = new int[slots.Count == 0 ? 0 : slots.Max(slot => slot.Component) + 1];
    }

    internal int EvaluatedStates => memo.Count;

    internal IReadOnlyList<Slot> Uncovered()
    {
        Coverage best = Search(0);
        return slots.Where((_, index) => !best.Covered[index]).ToArray();
    }

    private Coverage Search(int index)
    {
        if (index == slots.Count) return new Coverage(0, []);
        string key = StateKey(index);
        if (memo.TryGetValue(key, out Coverage? cached)) return cached;
        Coverage best = new(0, new bool[slots.Count - index]);
        Slot slot = slots[index];
        for (int group = 0; group < groups.Length; group++)
        {
            if (!CanUse(group, slot)) continue;
            best = CoverGroup(index, group, best);
            // Complete coverage is the unique optimal covered-slot sequence.
            if (best.Count == slots.Count - index) break;
        }
        if (best.Count < slots.Count - index)
        {
            Coverage skipped = Prepend(false, Search(index + 1));
            if (Better(skipped, best)) best = skipped;
        }
        memo.Add(key, best);
        return best;
    }

    private bool CanUse(int group, Slot slot) => remaining[group] != 0
        && (slot.Source is null || slot.Source == groups[group].Source)
        && Accepts(groups[group].Printed, slot.Required, slot.Printed);

    private Coverage CoverGroup(int index, int group, Coverage best)
    {
        Slot slot = slots[index];
        foreach (char resource in Declarations(slot, groups[group].Printed))
        {
            int bit = 1 << Resources.Types.IndexOf(resource);
            int previous = declared[slot.Component];
            if (slot.DistinctTypes && (previous & bit) != 0) continue;
            remaining[group]--;
            if (slot.DistinctTypes) declared[slot.Component] |= bit;
            Coverage candidate = Prepend(true, Search(index + 1));
            declared[slot.Component] = previous;
            remaining[group]++;
            if (Better(candidate, best)) best = candidate;
        }
        return best;
    }

    private string StateKey(int index) => string.Concat(
        index.ToString(System.Globalization.CultureInfo.InvariantCulture), ":",
        string.Join(',', remaining), ":", string.Join(',', declared));

    private static Coverage Prepend(bool covered, Coverage tail) =>
        new(tail.Count + (covered ? 1 : 0), [covered, .. tail.Covered]);

    // Repository policy: maximize covered slots, then prefer the supplied slot order.
    // Comparing the whole suffix keeps typed priority even across different icon assignments.
    private static bool Better(Coverage candidate, Coverage best)
    {
        if (candidate.Count != best.Count) return candidate.Count > best.Count;
        for (int index = 0; index < candidate.Covered.Length; index++)
            if (candidate.Covered[index] != best.Covered[index]) return candidate.Covered[index];
        return false;
    }

    private sealed record Coverage(int Count, bool[] Covered);
}
