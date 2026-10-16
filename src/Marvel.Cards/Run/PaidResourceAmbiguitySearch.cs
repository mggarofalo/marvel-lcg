using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Determines whether legal allocations disagree on resource-sensitive effects.</summary>
internal sealed class PaidResourceAmbiguitySearch(
    List<char> queried, string generated, int cost, string required)
{
    private readonly Dictionary<char, (bool Paid, bool NotPaid)> outcomes = queried.ToDictionary(
        resource => resource,
        _ => (Paid: false, NotPaid: false));
    private readonly char[] selected = new char[cost];

    internal bool IsAmbiguous() => queried.Count > 0 && Search(start: 0, chosen: 0);

    private bool Search(int start, int chosen)
    {
        if (chosen == selected.Length)
        {
            return DeclareWild(selected.ToArray(), index: 0);
        }

        int left = selected.Length - chosen;
        for (int index = start; index <= generated.Length - left; index++)
        {
            selected[chosen] = generated[index];
            if (Search(index + 1, chosen + 1))
            {
                return true;
            }
        }
        return false;
    }

    private bool DeclareWild(char[] declared, int index)
    {
        while (index < declared.Length && declared[index] != Resources.Wild) index++;
        if (index < declared.Length)
        {
            foreach (char declaration in Resources.Types)
            {
                declared[index] = declaration;
                if (DeclareWild(declared, index + 1)) return true;
            }
            declared[index] = Resources.Wild;
            return false;
        }
        if (!PaysRequirement(declared)) return false;
        return Observe(declared);
    }

    private bool PaysRequirement(char[] declared)
    {
        var pool = declared.ToList();
        foreach (char requiredType in required)
        {
            int found = pool.IndexOf(requiredType);
            if (found < 0) return false;
            pool.RemoveAt(found);
        }
        return true;
    }

    private bool Observe(char[] declared)
    {
        foreach (char resource in queried)
        {
            bool paid = declared.Contains(resource);
            var seen = outcomes[resource];
            outcomes[resource] = paid ? (true, seen.NotPaid) : (seen.Paid, true);
            if (outcomes[resource] is (true, true)) return true;
        }
        return false;
    }
}
