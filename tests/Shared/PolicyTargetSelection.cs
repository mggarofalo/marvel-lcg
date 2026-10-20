using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Tests;

/// <summary>Chooses deterministic test-policy targets using the offered engine constraints.</summary>
internal static class PolicyTargetSelection
{
    internal static IReadOnlyList<int> Select(World world, Affordance option) =>
        Select(option.Targets, world.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1);

    internal static IReadOnlyList<int> Select(TargetRequest? wanted, int villain = -1)
    {
        if (wanted is null) return [];
        if (wanted.IsGrouped && wanted.Groups is { Count: > 0 } groups) return groups[0];
        var ordered = wanted.Min == 1 && wanted.Legal.Contains(villain)
            ? new[] { villain }.Concat(wanted.Legal.Where(id => id != villain)) : wanted.Legal;
        return Allocate(wanted, ordered);
    }

    private static List<int> Allocate(TargetRequest wanted, IEnumerable<int> ordered)
    {
        var selected = new List<int>();
        foreach (int id in ordered)
            if (selected.Count < wanted.Min && wanted.AllowsPartial([.. selected, id])) selected.Add(id);
        while (selected.Count < wanted.Min)
        {
            int? next = ordered.Cast<int?>().FirstOrDefault(id => wanted.AllowsPartial([.. selected, id!.Value]));
            if (next is null) throw new InvalidOperationException("No offered target completes the test policy's allocation");
            selected.Add(next.Value);
        }
        if (!wanted.Allows(selected)) throw new InvalidOperationException("The test policy did not produce an admitted target selection");
        return selected;
    }
}
